using System.IO.Enumeration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CloudBackupData.Batch;

public static class CreationTools
{
    private static string FileInfoToS3Key(string initialDirectory, DirectoryInfo fileSystemBaseDirectory,
        FileInfo fileSystemFile)
    {
        return fileSystemFile.FullName
            .Replace($"{fileSystemBaseDirectory.FullName}\\", $"{initialDirectory}\\")
            .Replace("\\", "/");
    }

    public static async Task<List<S3RemoteFileAndMetadata>> GetAllCloudFiles(BackupJob job,
        IS3AccountInformation account, IProgress<string> progress)
    {
        return await S3Tools.ListS3Items(account,
            job.CloudDirectory.EndsWith("/") ? job.CloudDirectory : $"{job.CloudDirectory}/", progress);
    }

    public static async Task<List<DirectoryInfo>> GetAllLocalDirectories(BackupJob job)
    {
        var context = await CloudBackupContext.CreateInstance();

        var excludedDirectories = await context.ExcludedDirectories.Where(x => x.JobId == job.Id)
            .Select(x => x.Directory)
            .ToListAsync();

        var excludedDirectoryPatterns = await context.ExcludedDirectoryNamePatterns.Where(x => x.JobId == job.Id)
            .Select(x => x.Pattern).ToListAsync();

        var initialDirectory = new DirectoryInfo(job.LocalDirectory);

        return initialDirectory.AsList().Concat(GetAllLocalDirectories(new DirectoryInfo(job.LocalDirectory),
            excludedDirectories, excludedDirectoryPatterns)).ToList();
    }

    private static List<DirectoryInfo> GetAllLocalDirectories(DirectoryInfo searchDirectory,
        List<string> excludedDirectories,
        List<string> excludedNamePatterns)
    {
        var subDirectories = searchDirectory.GetDirectories().ToList();

        if (!subDirectories.Any()) return new List<DirectoryInfo>();

        var returnList = new List<DirectoryInfo>();

        foreach (var directoryInfo in subDirectories)
        {
            if (excludedDirectories.Contains(directoryInfo.FullName)) continue;

            if (excludedNamePatterns.Any(x => FileSystemName.MatchesSimpleExpression(x, directoryInfo.Name))) continue;

            returnList.Add(directoryInfo);

            returnList.AddRange(GetAllLocalDirectories(directoryInfo, excludedDirectories, excludedNamePatterns));
        }

        return returnList;
    }

    public static async Task<List<S3LocalFileAndMetadata>> GetAllLocalFiles(BackupJob job, IProgress<string> progress)
    {
        progress.Report("Local Files - Starting");
        var directories = await GetAllLocalDirectories(job);

        var context = await CloudBackupContext.CreateInstance();

        var excludedPatterns = await context.ExcludedFileNamePatterns.Where(x => x.JobId == job.Id)
            .Select(x => x.Pattern).OrderBy(x => x)
            .ToListAsync();

        var returnList = new List<S3LocalFileAndMetadata>();

        progress.Report($"Local Files - Getting Files - Found {directories.Count} Directories to Process");

        var counter = 0;

        foreach (var directoryInfo in directories)
        {
            counter++;

            var files = directoryInfo.GetFiles();

            foreach (var fileInfo in files)
            {
                if (excludedPatterns.Any(x => FileSystemName.MatchesSimpleExpression(x, fileInfo.Name))) continue;
                returnList.Add(await S3Tools.LocalFileAndMetadata(fileInfo));
            }

            if (counter % 50 == 0)
                progress.Report(
                    $"Local Files - {counter} of {directories.Count} Directories Complete - {returnList.Count} Files");
        }

        return returnList;
    }

    public static async Task<FileListAndChangeData> GetChanges(IS3AccountInformation accountInformation, BackupJob job,
        IProgress<string> progress)
    {
        var returnData = new FileListAndChangeData
        {
            Job = job,
            AccountInformation = accountInformation,
            S3Files = await GetAllCloudFiles(job, accountInformation, progress)
        };

        var localFiles = await GetAllLocalFiles(job, progress);
        var localFilesSystemBaseDirectory = new DirectoryInfo(job.LocalDirectory);

        returnData.FileSystemFiles = localFiles.Select(x => new S3FileSystemFileAndMetadataWithCloudKey(x.LocalFile,
                x.Metadata, FileInfoToS3Key(job.CloudDirectory, localFilesSystemBaseDirectory, x.LocalFile)))
            .ToList();

        progress.Report($"Change Check - {returnData.FileSystemFiles.Count} to process");
        var counter = 0;

        foreach (var loopFiles in returnData.FileSystemFiles)
        {
            counter++;


            var matchingFiles = returnData.S3Files.Where(x => x.Key == loopFiles.CloudKey).ToList();

            if (matchingFiles.Count == 0)
            {
                returnData.FileSystemFilesToUpload.Add(loopFiles);
                continue;
            }

            if (matchingFiles.Any(x =>
                    x.Metadata.LastWriteTime == loopFiles.Metadata.LastWriteTime &&
                    x.Metadata.FileSystemHash == loopFiles.Metadata.FileSystemHash)) continue;

            returnData.FileSystemFilesToUpload.Add(loopFiles);

            if (counter % 500 == 0)
                progress.Report(
                    $"Change Check - {counter} of {returnData.FileSystemFiles.Count} Files Checked - {returnData.FileSystemFilesToUpload} to Upload so far.");
        }

        returnData.S3FilesToDelete = returnData.S3Files
            .Where(x => returnData.FileSystemFiles.All(y => y.CloudKey != x.Key)).ToList();

        return returnData;
    }

    public static async Task<CloudTransferBatch> WriteChangesToDatabase(FileListAndChangeData changes)
    {
        var frozenNow = DateTime.Now.ToUniversalTime();
        var db = await CloudBackupContext.CreateInstance();

        var batch = new CloudTransferBatch { CreatedOn = frozenNow, JobId = changes.Job.Id };
        await db.CloudTransferBatches.AddAsync(batch);

        await db.SaveChangesAsync();

        await db.FileSystemFiles.AddRangeAsync(changes.FileSystemFiles.Select(x => new FileSystemFile
        {
            CreatedOn = frozenNow,
            FileHash = x.Metadata.FileSystemHash,
            FileSystemDateTime = x.Metadata.LastWriteTime,
            JobId = changes.Job.Id,
            CloudTransferBatchId = batch.Id,
        }));

        await db.CloudUploads.AddRangeAsync(changes.FileSystemFilesToUpload.Select(x => new CloudUpload
        {
            CreatedOn = frozenNow,
            CloudTransferBatchId = batch.Id,
            BucketName = changes.AccountInformation.BucketName(),
            FileSystemFile = x.LocalFile.FullName,
            CloudObjectKey = x.CloudKey,
            LastUpdatedOn = frozenNow
        }));

        await db.CloudDeletions.AddRangeAsync(changes.S3FilesToDelete.Select(x => new CloudDelete
        {
            CreatedOn = frozenNow,
            CloudTransferBatchId = batch.Id,
            BucketName = changes.AccountInformation.BucketName(),
            CloudObjectKey = x.Key,
            LastUpdatedOn = frozenNow
        }));

        await db.SaveChangesAsync();

        return batch;
    }
}