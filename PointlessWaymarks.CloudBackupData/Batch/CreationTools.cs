using System.IO.Enumeration;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;
using Serilog;

namespace PointlessWaymarks.CloudBackupData.Batch;

public static class CreationTools
{
    private static string FileInfoToS3Key(string initialDirectory, DirectoryInfo fileSystemBaseDirectory,
        FileInfo fileSystemFile)
    {
        if (!initialDirectory.EndsWith(@"/")) initialDirectory = $@"{initialDirectory}/";

        return fileSystemFile.FullName
            .Replace($"{fileSystemBaseDirectory.FullName}\\", $"{initialDirectory}")
            .Replace("\\", "/");
    }

    public static async Task<List<S3RemoteFileAndMetadata>> GetAllCloudFiles(string cloudDirectory,
        IS3AccountInformation account, IProgress<string> progress)
    {
        return await S3Tools.ListS3Items(account,
            cloudDirectory.EndsWith("/") ? cloudDirectory : $"{cloudDirectory}/", progress);
    }

    public static async Task<List<CloudBackupLocalDirectory>> GetAllLocalDirectories(int jobId,
        IProgress<string> progress)
    {
        progress.Report("Getting Directories - Querying Job From Db");

        var context = await CloudBackupContext.CreateInstance();

        var job = await context.BackupJobs.SingleAsync(x => x.Id == jobId);

        var excludedDirectories = job.ExcludedDirectories.Select(x => x.Directory)
            .ToList();

        var excludedDirectoryPatterns = job.ExcludedDirectoryNamePatterns.Select(x => x.Pattern).ToList();

        return await GetAllLocalDirectories(job.LocalDirectory, excludedDirectories, excludedDirectoryPatterns,
            progress);
    }

    public static Task<List<CloudBackupLocalDirectory>> GetAllLocalDirectories(string localDirectory,
        List<string> excludedDirectories, List<string> excludedDirectoryPatterns, IProgress<string> progress)
    {
        var initialDirectory = new CloudBackupLocalDirectory
            { Directory = new DirectoryInfo(localDirectory), Included = true };

        progress.Report($"Getting Directories - Initial Directory {initialDirectory.Directory.FullName}");

        return Task.FromResult(initialDirectory.AsList().Concat(GetSubdirectories(initialDirectory,
            excludedDirectories, excludedDirectoryPatterns, progress)).ToList());
    }

    public static async Task<FileListAndChangeData> GetChanges(IS3AccountInformation accountInformation,
        int backupJobId,
        IProgress<string> progress)
    {
        var db = await CloudBackupContext.CreateInstance();

        var job = await db.BackupJobs.SingleAsync(x => x.Id == backupJobId);

        var returnData = new FileListAndChangeData
        {
            Job = job,
            AccountInformation = accountInformation,
            S3Files = await GetAllCloudFiles(job.CloudDirectory, accountInformation, progress)
        };

        var localFiles = await GetIncludedLocalFiles(job.Id, progress);
        var localFilesSystemBaseDirectory = new DirectoryInfo(job.LocalDirectory);

        returnData.FileSystemFiles = localFiles.Select(x => new S3FileSystemFileAndMetadataWithCloudKey(x.LocalFile,
                x.UploadMetadata, FileInfoToS3Key(job.CloudDirectory, localFilesSystemBaseDirectory, x.LocalFile)))
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
                    x.Metadata.FileSystemHash == loopFiles.UploadMetadata.FileSystemHash)) continue;

            returnData.FileSystemFilesToUpload.Add(loopFiles);

            if (counter % 500 == 0)
                progress.Report(
                    $"Change Check - {counter} of {returnData.FileSystemFiles.Count} Files Checked - {returnData.FileSystemFilesToUpload} to Upload so far.");
        }

        returnData.S3FilesToDelete = returnData.S3Files
            .Where(x => returnData.FileSystemFiles.All(y => y.CloudKey != x.Key)).ToList();

        return returnData;
    }

    public static async Task<List<S3LocalFileAndMetadata>> GetExcludedLocalFiles(int backupJobId,
        IProgress<string> progress)
    {
        var db = await CloudBackupContext.CreateInstance();

        var job = await db.BackupJobs.SingleAsync(x => x.Id == backupJobId);

        var excludedDirectories = job.ExcludedDirectories.Select(x => x.Directory).OrderBy(x => x).ToList();
        var excludedDirectoryPatterns =
            job.ExcludedDirectoryNamePatterns.Select(x => x.Pattern).OrderBy(x => x).ToList();
        var excludedFilePatterns = job.ExcludedFileNamePatterns.Select(x => x.Pattern).OrderBy(x => x).ToList();

        return await GetExcludedLocalFiles(job.LocalDirectory, excludedDirectories, excludedDirectoryPatterns,
            excludedFilePatterns, progress);
    }

    public static async Task<List<S3LocalFileAndMetadata>> GetExcludedLocalFiles(string initialDirectory,
        List<string> excludedDirectories, List<string> excludedDirectoryPatterns, List<string> excludedFilePatterns,
        IProgress<string> progress)
    {
        progress.Report("Local Files - Starting");

        var directories =
            await GetAllLocalDirectories(initialDirectory, excludedDirectories, excludedDirectoryPatterns, progress);

        var returnList = new List<S3LocalFileAndMetadata>();

        progress.Report($"Local Files - Getting Files - Found {directories.Count} Directories to Process");

        var counter = 0;

        foreach (var directoryInfo in directories)
        {
            counter++;

            var files = directoryInfo.Directory.GetFiles();

            foreach (var fileInfo in files)
                if (!directoryInfo.Included || (directoryInfo.Included &&
                                                excludedFilePatterns.Any(x =>
                                                    FileSystemName.MatchesSimpleExpression(x, fileInfo.Name))))
                    returnList.Add(await S3Tools.LocalFileAndMetadata(fileInfo));

            if (counter % 50 == 0)
                progress.Report(
                    $"Local Files - {counter} of {directories.Count} Directories Complete - {returnList.Count} Files");
        }

        return returnList;
    }

    public static async Task<List<S3LocalFileAndMetadata>> GetIncludedLocalFiles(int backupJobId,
        IProgress<string> progress)
    {
        var db = await CloudBackupContext.CreateInstance();

        var job = await db.BackupJobs.SingleAsync(x => x.Id == backupJobId);

        var excludedDirectories = job.ExcludedDirectories.Select(x => x.Directory).OrderBy(x => x).ToList();
        var excludedDirectoryPatterns =
            job.ExcludedDirectoryNamePatterns.Select(x => x.Pattern).OrderBy(x => x).ToList();
        var excludedFilePatterns = job.ExcludedFileNamePatterns.Select(x => x.Pattern).OrderBy(x => x).ToList();

        return await GetIncludedLocalFiles(job.LocalDirectory, excludedDirectories, excludedDirectoryPatterns,
            excludedFilePatterns, progress);
    }

    public static async Task<List<S3LocalFileAndMetadata>> GetIncludedLocalFiles(string initialDirectory,
        List<string> excludedDirectories, List<string> excludedDirectoryPatterns, List<string> excludedFilePatterns,
        IProgress<string> progress)
    {
        Log.ForContext(nameof(initialDirectory), initialDirectory)
            .ForContext(nameof(excludedDirectories), excludedDirectories, true)
            .ForContext(nameof(excludedDirectoryPatterns), excludedDirectoryPatterns, true)
            .ForContext(nameof(excludedFilePatterns), excludedFilePatterns, true)
            .Information("Local Files Listing and S3 Metadata - Starting");

        var directories =
            (await GetAllLocalDirectories(initialDirectory, excludedDirectories, excludedDirectoryPatterns, progress))
            .Where(x => x.Included).Select(x => x.Directory).OrderBy(x => x.FullName).ToList();

        var returnList = new List<S3LocalFileAndMetadata>();

        progress.Report(
            $"Local Files - Getting Included Files - Found {directories.Count} Included Directories to Process");

        var counter = 0;

        foreach (var directoryInfo in directories)
        {
            counter++;

            var files = directoryInfo.GetFiles();

            foreach (var fileInfo in files)
            {
                if (excludedFilePatterns.Any(x => FileSystemName.MatchesSimpleExpression(x, fileInfo.Name))) continue;
                returnList.Add(await S3Tools.LocalFileAndMetadata(fileInfo));
            }

            if (counter % 50 == 0)
                progress.Report(
                    $"Local Files - {counter} of {directories.Count} Directories Complete - {returnList.Count} Files - {DateTime.Now:t}");
        }

        return returnList;
    }

    private static List<CloudBackupLocalDirectory> GetSubdirectories(CloudBackupLocalDirectory searchLocalDirectory,
        List<string> excludedDirectories, List<string> excludedNamePatterns, IProgress<string> progress)
    {
        var subDirectories = searchLocalDirectory.Directory.GetDirectories().ToList();

        if (!subDirectories.Any()) return new List<CloudBackupLocalDirectory>();

        var returnList = new List<CloudBackupLocalDirectory>();

        var parentDirectoryIsExcluded = !searchLocalDirectory.Included;

        foreach (var directoryInfo in subDirectories)
        {
            var toAdd = new CloudBackupLocalDirectory
            {
                Directory = directoryInfo,
                Included = !(excludedDirectories.Contains(directoryInfo.FullName)
                             || excludedNamePatterns.Any(x =>
                                 FileSystemName.MatchesSimpleExpression(x, directoryInfo.Name))
                             || parentDirectoryIsExcluded)
            };

            returnList.Add(toAdd);

            returnList.AddRange(GetSubdirectories(toAdd, excludedDirectories, excludedNamePatterns, progress));
        }

        return returnList;
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
            FileHash = x.UploadMetadata.FileSystemHash,
            FileName = x.LocalFile.FullName,
            FileSystemDateTime = x.UploadMetadata.LastWriteTime,
            FileSize = x.LocalFile.Length,
            JobId = changes.Job.Id,
            CloudTransferBatchId = batch.Id
        }));

        await db.CloudFiles.AddRangeAsync(changes.S3Files.Select(x => new CloudFile
        {
            CreatedOn = frozenNow,
            FileHash = x.Metadata.FileSystemHash,
            Key = x.Key,
            FileSystemDateTime = x.Metadata.LastWriteTime,
            FileSize = x.Metadata.FileSize,
            JobId = changes.Job.Id,
            CloudTransferBatchId = batch.Id
        }));

        await db.CloudUploads.AddRangeAsync(changes.FileSystemFilesToUpload.Select(x => new CloudUpload
        {
            CreatedOn = frozenNow,
            CloudTransferBatchId = batch.Id,
            BucketName = changes.AccountInformation.BucketName(),
            FileSystemFile = x.LocalFile.FullName,
            FileSize = x.LocalFile.Length,
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