using System.Collections.Concurrent;
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
    
    public static async Task<List<S3RemoteFileAndMetadata>> GetAllCloudFiles(int backupJobId, string cloudDirectory,
        IS3AccountInformation account, IProgress<string> progress)
    {
        var db = await CloudBackupContext.CreateReportingInstance();
        
        progress.Report("Deleting Db Cloud Cache Files for Full Refresh");
        await db.CloudCacheFiles.Where(x => x.JobId == backupJobId).ExecuteDeleteAsync(CancellationToken.None);
        
        var cloudFiles = await S3Tools.ListS3Items(account,
            cloudDirectory.EndsWith("/") ? cloudDirectory : $"{cloudDirectory}/", progress);
        
        var frozenNow = DateTime.Now;
        
        var dbEntryChunks = cloudFiles.Select(x =>
            S3RemoteFileAndMetadataToCloudFile(backupJobId, x, frozenNow, $"{frozenNow:s} Scan;")).ToList().Chunk(250).ToList();
        
        var dbSaveCounter = 0;
        foreach (var loopDbChunk in dbEntryChunks)
        {
            if(++dbSaveCounter % 25 == 0) Console.WriteLine($"Saving Db Cloud Cache Files - {dbSaveCounter} of {dbEntryChunks.Count}");
            db.CloudCacheFiles.AddRange(loopDbChunk);
            await db.SaveChangesAsync();
        }

        return cloudFiles;
    }
    
    public static async Task<List<CloudBackupLocalDirectory>> GetAllLocalDirectories(int jobId,
        IProgress<string> progress)
    {
        progress.Report("Getting Directories - Querying Job From Db");
        
        var context = await CloudBackupContext.CreateReportingInstance();
        
        var job = await context.BackupJobs.Include(backupJob => backupJob.ExcludedDirectories)
            .Include(backupJob => backupJob.ExcludedDirectoryNamePatterns).SingleAsync(x => x.Id == jobId);
        
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
    
    public static async Task<FileListAndChangeData> GetChanges(
        IS3AccountInformation accountInformation,
        int backupJobId, bool basedOnCloudCacheFiles,
        IProgress<string> progress)
    {
        Log.Information($"Cloud Backup - Getting Changes - Use Cache: {basedOnCloudCacheFiles}");
        
        var db = await CloudBackupContext.CreateReportingInstance();
        
        var job = await db.BackupJobs.SingleAsync(x => x.Id == backupJobId);
        
        var cloudFiles = basedOnCloudCacheFiles
            ? db.CloudCacheFiles.Where(x => x.JobId == job.Id).Select(x => new S3RemoteFileAndMetadata(x.Bucket, x.CloudObjectKey,
                new S3StandardMetadata(x.FileSystemDateTime, x.FileHash, x.FileSize))).ToList()
            : await GetAllCloudFiles(backupJobId, job.CloudDirectory, accountInformation, progress);
        
        var returnData = new FileListAndChangeData
        {
            Job = job,
            AccountInformation = accountInformation,
            S3Files = cloudFiles,
            ChangesBasedOnNewCloudFileScan = false
        };
        
        var localFiles = await GetIncludedLocalFiles(job.Id, progress);
        var localFilesSystemBaseDirectory = new DirectoryInfo(job.LocalDirectory);
        
        returnData.FileSystemFiles = localFiles.Select(x => new S3FileSystemFileAndMetadataWithCloudKey(x.LocalFile,
                x.UploadMetadata, FileInfoToS3Key(job.CloudDirectory, localFilesSystemBaseDirectory, x.LocalFile)))
            .ToList();
        
        progress.Report($"Change Check - {returnData.FileSystemFiles.Count} to process");
        var counter = 0;
        
        var hashGroupedFileSystemFiles =
            returnData.FileSystemFiles.GroupBy(x => x.UploadMetadata.FileSystemHash).ToList();
        
        var singleFileHashGroup = hashGroupedFileSystemFiles.Where(x => x.Count() == 1).Select(x => x.First()).ToList();
        
        var singleHashCopyContainer = new ConcurrentBag<S3CopyInformation>();
        var singleHashUploadContainer = new ConcurrentBag<S3FileSystemFileAndMetadataWithCloudKey>();

        await Parallel.ForEachAsync(singleFileHashGroup, async (loopFiles, _) =>
        {
            var frozenCounter = Interlocked.Increment(ref counter);
            if (++frozenCounter % 500 == 0 || frozenCounter == 1)
                progress.Report(
                    $"Change Check - {frozenCounter} of {singleFileHashGroup.Count} Single Hash Files Checked - {singleHashUploadContainer.Count} to Upload, {singleHashCopyContainer.Count} to Copy.");
            
            var keyMatchingFiles = returnData.S3Files.Where(x => x.Key == loopFiles.CloudKey).ToList();
            
            if (keyMatchingFiles.Count == 0)
            {
                var hashMatchFiles = returnData.S3Files
                    .Where(x => x.Metadata.FileSystemHash == loopFiles.UploadMetadata.FileSystemHash).ToList();
                if (hashMatchFiles.Any())
                {
                    singleHashCopyContainer.Add(new S3CopyInformation(loopFiles, hashMatchFiles.First()));
                    return;
                }
                
                singleHashUploadContainer.Add(loopFiles);
                return;
            }
            
            if (keyMatchingFiles.Any(x =>
                    x.Metadata.FileSystemHash == loopFiles.UploadMetadata.FileSystemHash)) return;
            
            singleHashUploadContainer.Add(loopFiles);
        });

        returnData.FileSystemFilesToUpload = singleHashUploadContainer.ToList();
        returnData.S3FilesToCopy = singleHashCopyContainer.ToList();

        var multiFileHashGroup = hashGroupedFileSystemFiles.Where(x => x.Count() > 1).ToList();
        
        counter = 0;
        
        var multiHashCopyContainer = new ConcurrentBag<S3CopyInformation>();
        var multiHashUploadContainer = new ConcurrentBag<S3FileSystemFileAndMetadataWithCloudKey>();

        await Parallel.ForEachAsync(multiFileHashGroup, async (loopFileGroup, _) =>
        {
            var frozenCounter = Interlocked.Increment(ref counter);
            
            if (++frozenCounter % 500 == 0 || frozenCounter == 1)
                progress.Report(
                    $"Change Check - {frozenCounter} of {multiFileHashGroup.Count} Multiple Copy Files Checked - {multiHashUploadContainer.Count} to Upload, {multiHashCopyContainer.Count} to Copy.");

            var allKeys = loopFileGroup.Select(x => x.CloudKey).ToList();

            var groupKeysMatchingFiles = returnData.S3Files.Where(x => allKeys.Contains(x.Key)).ToList();

            //Case - all matches - nothing to do
            if (loopFileGroup.Count() == groupKeysMatchingFiles.Count) return;

            //Case - no matches - upload all
            if (!groupKeysMatchingFiles.Any())
            {
                loopFileGroup.ToList().ForEach(x => multiHashUploadContainer.Add(x));
                return;
            }

            foreach (var loopFiles in loopFileGroup)
            {
                var keyMatchingFiles = returnData.S3Files.Where(x => x.Key == loopFiles.CloudKey).ToList();

                if (keyMatchingFiles.Count == 0)
                {
                    var hashMatchFiles = returnData.S3Files
                        .Where(x => x.Metadata.FileSystemHash == loopFiles.UploadMetadata.FileSystemHash).ToList();
                    if (hashMatchFiles.Any())
                    {
                        multiHashCopyContainer.Add(new S3CopyInformation(loopFiles, hashMatchFiles.First()));
                        Log.Information(
                            "Cloud Backup - File {file} has a Hash Match but no Key Match - {hashMatchFiles}",
                            loopFiles.LocalFile.FullName, hashMatchFiles);
                        continue;
                    }

                    multiHashUploadContainer.Add(loopFiles);
                    continue;
                }

                if (keyMatchingFiles.Any(x =>
                        x.Metadata.FileSystemHash == loopFiles.UploadMetadata.FileSystemHash)) continue;
                
                multiHashUploadContainer.Add(loopFiles);
            }

        });
        
        returnData.S3FilesToCopy.AddRange(multiHashCopyContainer);
        returnData.FileSystemFilesToUpload.AddRange(multiHashUploadContainer);
        
        returnData.S3FilesToDelete = returnData.S3Files
            .Where(x => returnData.FileSystemFiles.All(y => y.CloudKey != x.Key)).ToList();
        
        Log.Information(
            "Cloud Backup - Found {uploadCount} Uploads and {deleteCount} Deletes based on Cloud Cache and Local Scan",
            returnData.FileSystemFilesToUpload.Count, returnData.S3FilesToDelete.Count);
        
        return returnData;
    }
    
    public static async Task<List<S3LocalFileAndMetadata>> GetExcludedLocalFiles(int backupJobId,
        IProgress<string> progress)
    {
        var db = await CloudBackupContext.CreateReportingInstance();
        
        var job = await db.BackupJobs.Include(backupJob => backupJob.ExcludedDirectories)
            .Include(backupJob => backupJob.ExcludedDirectoryNamePatterns)
            .Include(backupJob => backupJob.ExcludedFileNamePatterns).SingleAsync(x => x.Id == backupJobId);
        
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
        
        progress.Report($"Local Files - Getting Files - Found {directories.Count} Directories to Process");
        
        var counter = 0;
        
        var returnContainer = new ConcurrentBag<S3LocalFileAndMetadata>();

        await Parallel.ForEachAsync(directories, async (info, _) =>
        {
            var frozenCounter = Interlocked.Increment(ref counter);
            
            if (counter % 50 == 0 || frozenCounter == 1)
                progress.Report(
                    $"Local Files - {counter} of {directories.Count} Directories Complete - {returnContainer.Count} Files");

            var files = info.Directory.GetFiles();
            
            foreach (var fileInfo in files)
            {
                if (!info.Included || excludedFilePatterns.Any(x => FileSystemName.MatchesSimpleExpression(x, fileInfo.Name)))
                    returnContainer.Add(await S3Tools.LocalFileAndMetadata(fileInfo));
            }
        });
        
        return returnContainer.OrderByDescending(x => x.LocalFile.FullName.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar)).ThenBy(x => x.LocalFile.FullName).ToList();
    }
    
    public static async Task<List<S3LocalFileAndMetadata>> GetIncludedLocalFiles(int backupJobId,
        IProgress<string> progress)
    {
        var db = await CloudBackupContext.CreateReportingInstance();
        
        var job = await db.BackupJobs.Include(backupJob => backupJob.ExcludedDirectories)
            .Include(backupJob => backupJob.ExcludedDirectoryNamePatterns)
            .Include(backupJob => backupJob.ExcludedFileNamePatterns).SingleAsync(x => x.Id == backupJobId);
        
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
        
        var fileContainer = new ConcurrentBag<S3LocalFileAndMetadata>();
        
        progress.Report(
            $"Local Files - Getting Included Files - Found {directories.Count} Included Directories to Process");
        
        var counter = 0;
        
        await Parallel.ForEachAsync(directories, async (info, _) =>
        {
            var frozenCounter = Interlocked.Increment(ref counter);
            
            if (frozenCounter % 50 == 0 || frozenCounter == 1)
                progress.Report(
                    $"Local Files - {frozenCounter} of {directories.Count} Directories Complete - {fileContainer.Count} Files - {DateTime.Now:t}");

            var files = info.GetFiles();
            
            foreach (var fileInfo in files)
            {
                if (excludedFilePatterns.Any(x => FileSystemName.MatchesSimpleExpression(x, fileInfo.Name))) continue;
                fileContainer.Add(await S3Tools.LocalFileAndMetadata(fileInfo));
            }
        });
        
        return fileContainer.OrderByDescending(x => x.LocalFile.FullName.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar)).ThenBy(x => x.LocalFile.FullName).ToList();
    }
    
    private static List<CloudBackupLocalDirectory> GetSubdirectories(CloudBackupLocalDirectory searchLocalDirectory,
        List<string> excludedDirectories, List<string> excludedNamePatterns, IProgress<string> progress)
    {
        var subDirectories = searchLocalDirectory.Directory.GetDirectories().ToList();
        
        if (!subDirectories.Any()) return [];
        
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
    
    public static CloudCacheFile S3RemoteFileAndMetadataToCloudFile(int jobId, S3RemoteFileAndMetadata toMap,
        DateTime lastEditOn, string note)
    {
        return new CloudCacheFile
        {
            Bucket = toMap.Bucket,
            FileSize = toMap.Metadata.FileSize,
            FileHash = toMap.Metadata.FileSystemHash,
            JobId = jobId,
            CloudObjectKey = toMap.Key,
            LastEditOn = lastEditOn,
            FileSystemDateTime = toMap.Metadata.LastWriteTime,
            Note = note
        };
    }
    
    public static async Task<CloudTransferBatch> WriteChangesToDatabase(FileListAndChangeData changes)
    {
        var frozenNow = DateTime.Now.ToUniversalTime();
        var db = await CloudBackupContext.CreateReportingInstance();
        
        var batch = new CloudTransferBatch
        {
            CreatedOn = frozenNow, JobId = changes.Job.Id,
            BasedOnNewCloudFileScan = changes.ChangesBasedOnNewCloudFileScan
        };
        await db.CloudTransferBatches.AddAsync(batch);
        
        await db.SaveChangesAsync();
        
        await db.FileSystemFiles.AddRangeAsync(changes.FileSystemFiles.Select(x => new FileSystemFile
        {
            CreatedOn = frozenNow,
            FileHash = x.UploadMetadata.FileSystemHash,
            FileName = x.LocalFile.FullName,
            FileSystemDateTime = x.UploadMetadata.LastWriteTime,
            FileSize = x.LocalFile.Length,
            CloudTransferBatchId = batch.Id
        }));
        
        await db.CloudFiles.AddRangeAsync(changes.S3Files.Select(x => new CloudFile
        {
            CreatedOn = frozenNow,
            FileHash = x.Metadata.FileSystemHash,
            CloudObjectKey = x.Key,
            FileSystemDateTime = x.Metadata.LastWriteTime,
            FileSize = x.Metadata.FileSize,
            CloudTransferBatchId = batch.Id
        }));
        
        await db.CloudCopies.AddRangeAsync(changes.S3FilesToCopy.Select(x => new CloudCopy()
        {
            CreatedOn = frozenNow,
            CloudTransferBatchId = batch.Id,
            BucketName = changes.AccountInformation.BucketName(),
            FileSystemFile = x.LocalFile.LocalFile.FullName,
            FileSize = x.LocalFile.LocalFile.Length,
            ExistingCloudObjectKey = x.ExistingRemoteFile.Key,
            NewCloudObjectKey = x.LocalFile.CloudKey,
            LastUpdatedOn = frozenNow
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
            FileSize = x.Metadata.FileSize,
            LastUpdatedOn = frozenNow
        }));
        
        await db.SaveChangesAsync();
        
        Log.Information(
            "New Batch Created - Id {batchId}, New Cloud Scan {newCloudScan}, {uploadCount} Uploads, {deleteCount} Deletes, {localFilesCount} Local Files, {cloudFilesCount}",
            batch.Id, batch.BasedOnNewCloudFileScan, batch.CloudUploads.Count, batch.CloudDeletions.Count,
            batch.FileSystemFiles.Count, batch.CloudFiles.Count);
        
        DataNotifications.PublishDataNotification(nameof(CreationTools), DataNotificationContentType.BackupJob,
            DataNotificationUpdateType.New, changes.Job.PersistentId, batch.Id);
        
        return batch;
    }
}