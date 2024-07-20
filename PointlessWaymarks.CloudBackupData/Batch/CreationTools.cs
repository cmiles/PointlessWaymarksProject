using System.Collections.Concurrent;
using System.Data.SQLite;
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
        if (!initialDirectory.EndsWith("/")) initialDirectory = $"{initialDirectory}/";
        
        return fileSystemFile.FullName
            .Replace($"{fileSystemBaseDirectory.FullName}\\", $"{initialDirectory}")
            .Replace("\\", "/");
    }
    
    public static async Task<List<S3RemoteFileAndMetadata>> GetAllCloudFiles(int backupJobId, string cloudDirectory,
        IS3AccountInformation account, IProgress<string> progress)
    {
        var db = await CloudBackupContext.CreateInstance();
        
        progress.Report("Deleting Db Cloud Cache Files for Full Refresh");
        await db.CloudCacheFiles.Where(x => x.BackupJobId == backupJobId).ExecuteDeleteAsync(CancellationToken.None);
        
        var cloudFiles = await S3Tools.ListS3Items(account,
            cloudDirectory.EndsWith("/") ? cloudDirectory : $"{cloudDirectory}/", progress);
        
        var frozenNow = DateTime.Now;
        
        var cloudFilesContainer = new ConcurrentBag<CloudCacheFile>();
        var cloudFileCounter = 0;
        
        Parallel.ForEach(cloudFiles, (cloudFile, _) =>
        {
            var frozenCounter = Interlocked.Increment(ref cloudFileCounter);
            
            if (frozenCounter % 1000 == 0 || frozenCounter == 1)
                progress.Report($"Cloud Files - {frozenCounter} of {cloudFiles.Count} Files Processed");
            
            cloudFilesContainer.Add(
                S3RemoteFileAndMetadataToCloudFile(backupJobId, cloudFile, frozenNow, $"{frozenNow:s} Scan;"));
        });
        
        var cloudFileList = cloudFilesContainer.ToList();
        
        await using var connection = new SQLiteConnection($"Data Source={CloudBackupContext.CurrentDatabaseFileName}");
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();
        
        progress.Report("Adding Cloud Files to Db");
        
        await using (var command = new SQLiteCommand(connection))
        {
            command.CommandText =
                "INSERT INTO CloudCacheFiles (Bucket, CloudObjectKey, FileHash, FileSize, FileSystemDateTime, BackupJobId, LastEditOn, Note) VALUES (@Bucket, @CloudObjectKey, @FileHash, @FileSize, @FileSystemDateTime, @BackupJobId, @LastEditOn, @Note)";
            command.Prepare();
            
            foreach (var file in cloudFileList)
            {
                command.Parameters.AddWithValue("@Bucket", file.Bucket);
                command.Parameters.AddWithValue("@CloudObjectKey", file.CloudObjectKey);
                command.Parameters.AddWithValue("@FileHash", file.FileHash);
                command.Parameters.AddWithValue("@FileSize", file.FileSize);
                command.Parameters.AddWithValue("@FileSystemDateTime", file.FileSystemDateTime);
                command.Parameters.AddWithValue("@BackupJobId", file.BackupJobId);
                command.Parameters.AddWithValue("@LastEditOn", file.LastEditOn.ToString("o"));
                command.Parameters.AddWithValue("@Note", file.Note);
                
                await command.ExecuteNonQueryAsync();
                command.Parameters.Clear();
            }
        }
        
        progress.Report("Committing Cloud Files to Db");
        
        transaction.Commit();
        
        var job = await db.BackupJobs.SingleAsync(x => x.Id == backupJobId);
        job.LastCloudFileScan = frozenNow;
        await db.SaveChangesAsync();

        return cloudFiles;
    }
    
    public static async Task<List<CloudBackupLocalDirectory>> GetAllLocalDirectories(int jobId,
        IProgress<string> progress)
    {
        progress.Report("Getting Directories - Querying Job From Db");
        
        var context = await CloudBackupContext.CreateInstance();
        
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
        
        var db = await CloudBackupContext.CreateInstance();
        
        var job = await db.BackupJobs.SingleAsync(x => x.Id == backupJobId);
        
        var cloudFiles = basedOnCloudCacheFiles
            ? db.CloudCacheFiles.Where(x => x.BackupJobId == job.Id).Select(x => new S3RemoteFileAndMetadata(x.Bucket,
                x.CloudObjectKey,
                new S3StandardMetadata(x.FileSystemDateTime, x.FileHash, x.FileSize))).ToList()
            : await GetAllCloudFiles(backupJobId, job.CloudDirectory, accountInformation, progress);
        
        var returnData = new FileListAndChangeData
        {
            Job = job,
            AccountInformation = accountInformation,
            S3Files = cloudFiles,
            ChangesBasedOnNewCloudFileScan = !basedOnCloudCacheFiles
        };
        
        var localFiles = await GetIncludedLocalFiles(job.Id, progress);
        var localFilesSystemBaseDirectory = new DirectoryInfo(job.LocalDirectory);
        
        var fileSystemFilesContainer = new ConcurrentBag<S3FileSystemFileAndMetadataWithCloudKey>();
        
        var fileMetadataCounter = 0;
        
        Parallel.ForEach(localFiles, x =>
        {
            var frozenFileMetadataCounter = Interlocked.Increment(ref fileMetadataCounter);
            
            if (frozenFileMetadataCounter % 500 == 0 || frozenFileMetadataCounter == 1)
                progress.Report(
                    $"Metadata Transform - {frozenFileMetadataCounter} of {localFiles.Count} Local Files Processed - {fileSystemFilesContainer.Count} to Process");
            
            fileSystemFilesContainer.Add(new S3FileSystemFileAndMetadataWithCloudKey(x.LocalFile,
                x.UploadMetadata, FileInfoToS3Key(job.CloudDirectory, localFilesSystemBaseDirectory, x.LocalFile)));
        });
        
        
        returnData.FileSystemFiles = fileSystemFilesContainer.ToList();
        
        progress.Report($"Change Check - {returnData.FileSystemFiles.Count} to process");
        var singleFileCounter = 0;
        
        var hashGroupedFileSystemFiles =
            returnData.FileSystemFiles.GroupBy(x => x.UploadMetadata.FileSystemHash).ToList();
        
        var singleFileHashGroup = hashGroupedFileSystemFiles.Where(x => x.Count() == 1).Select(x => x.First()).ToList();
        
        var singleHashCopyContainer = new ConcurrentBag<S3CopyInformation>();
        var singleHashUploadContainer = new ConcurrentBag<S3FileSystemFileAndMetadataWithCloudKey>();
        
        Parallel.ForEach(singleFileHashGroup, (loopFiles, _) =>
        {
            var frozenCounter = Interlocked.Increment(ref singleFileCounter);
            if (frozenCounter % 500 == 0 || frozenCounter == 1)
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
        
        var multiFileCounter = 0;
        
        var multiHashCopyContainer = new ConcurrentBag<S3CopyInformation>();
        var multiHashUploadContainer = new ConcurrentBag<S3FileSystemFileAndMetadataWithCloudKey>();
        
        Parallel.ForEach(multiFileHashGroup, (loopFileGroup, _) =>
        {
            var frozenCounter = Interlocked.Increment(ref multiFileCounter);
            
            if (frozenCounter % 500 == 0 || frozenCounter == 1)
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
        var db = await CloudBackupContext.CreateInstance();
        
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
            
            var files = info.Directory.GetFiles();
            
            foreach (var fileInfo in files)
                if (!info.Included ||
                    excludedFilePatterns.Any(x => FileSystemName.MatchesSimpleExpression(x, fileInfo.Name)))
                    returnContainer.Add(await S3Tools.LocalFileAndMetadata(fileInfo));
            
            if (counter % 50 == 0 || frozenCounter == 1)
                progress.Report(
                    $"Local Files - {counter} of {directories.Count} Directories Complete - {returnContainer.Count} Files");
        });
        
        return returnContainer
            .OrderByDescending(x =>
                x.LocalFile.FullName.Count(c =>
                    c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar))
            .ThenBy(x => x.LocalFile.FullName).ToList();
    }
    
    public static async Task<List<S3LocalFileAndMetadata>> GetIncludedLocalFiles(int backupJobId,
        IProgress<string> progress)
    {
        var db = await CloudBackupContext.CreateInstance();
        
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
        
        return fileContainer
            .OrderByDescending(x =>
                x.LocalFile.FullName.Count(c =>
                    c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar))
            .ThenBy(x => x.LocalFile.FullName).ToList();
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
            BackupJobId = jobId,
            CloudObjectKey = toMap.Key,
            LastEditOn = lastEditOn,
            FileSystemDateTime = toMap.Metadata.LastWriteTime,
            Note = note
        };
    }
    
    public static async Task<CloudTransferBatchInformation> WriteChangesToDatabase(FileListAndChangeData changes,
        IProgress<string> progress)
    {
        var frozenNow = DateTime.Now;
        var db = await CloudBackupContext.CreateInstance();
        
        progress.Report($"Creating a new Cloud Transfer Batch - {frozenNow}");
        
        var batch = new CloudTransferBatch
        {
            CreatedOn = frozenNow, BackupJobId = changes.Job.Id,
            BasedOnNewCloudFileScan = changes.ChangesBasedOnNewCloudFileScan
        };
        await db.CloudTransferBatches.AddAsync(batch);
        
        await db.SaveChangesAsync();
        
        var batchId = batch.Id;

        progress.Report("Batch Created - Starting Bulk Transaction");
        
        progress.Report($"Adding {changes.FileSystemFiles.Count} File System Files");
        
        await using var connection = new SQLiteConnection($"Data Source={CloudBackupContext.CurrentDatabaseFileName}");
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();
        
        await using (var command = new SQLiteCommand(connection))
        {
            command.CommandText =
                "INSERT INTO FileSystemFiles (CreatedOn, FileHash, FileName, FileSystemDateTime, FileSize, CloudTransferBatchId) VALUES (@CreatedOn, @FileHash, @FileName, @FileSystemDateTime, @FileSize, @CloudTransferBatchId)";
            
            command.Prepare();
            
            foreach (var file in changes.FileSystemFiles)
            {
                command.Parameters.AddWithValue("@CreatedOn", frozenNow.ToString("o"));
                command.Parameters.AddWithValue("@FileHash", file.UploadMetadata.FileSystemHash);
                command.Parameters.AddWithValue("@FileName", file.LocalFile.FullName);
                command.Parameters.AddWithValue("@FileSystemDateTime", file.UploadMetadata.LastWriteTime);
                command.Parameters.AddWithValue("@FileSize", file.LocalFile.Length);
                command.Parameters.AddWithValue("@CloudTransferBatchId", batchId);
                
                await command.ExecuteNonQueryAsync();
            }
        }
        
        progress.Report($"Adding {changes.S3Files.Count} Cloud Files");
        
        await using (var command = new SQLiteCommand(connection))
        {
            command.CommandText =
                "INSERT INTO CloudFiles (CreatedOn, FileHash, CloudObjectKey, FileSystemDateTime, FileSize, CloudTransferBatchId) VALUES (@CreatedOn, @FileHash, @CloudObjectKey, @FileSystemDateTime, @FileSize, @CloudTransferBatchId)";
            command.Prepare();
            
            foreach (var file in changes.S3Files)
            {
                command.Parameters.AddWithValue("@CreatedOn", frozenNow.ToString("o"));
                command.Parameters.AddWithValue("@FileHash", file.Metadata.FileSystemHash);
                command.Parameters.AddWithValue("@CloudObjectKey", file.Key);
                command.Parameters.AddWithValue("@FileSystemDateTime", file.Metadata.LastWriteTime);
                command.Parameters.AddWithValue("@FileSize", file.Metadata.FileSize);
                command.Parameters.AddWithValue("@CloudTransferBatchId", batchId);
                
                await command.ExecuteNonQueryAsync();
            }
        }
        
        progress.Report($"Adding {changes.S3FilesToCopy.Count} S3 Files to Copy");
        
        await using (var command = new SQLiteCommand(connection))
        {
            command.CommandText =
                "INSERT INTO CloudCopies (BucketName, CloudTransferBatchId, CopyCompletedSuccessfully, CreatedOn, ErrorMessage, ExistingCloudObjectKey, FileSize, FileSystemFile, LastUpdatedOn, NewCloudObjectKey) VALUES (@BucketName, @CloudTransferBatchId, @CopyCompletedSuccessfully, @CreatedOn, @ErrorMessage, @ExistingCloudObjectKey, @FileSize, @FileSystemFile, @LastUpdatedOn, @NewCloudObjectKey)";
            command.Prepare();
            
            foreach (var file in changes.S3FilesToCopy)
            {
                command.Parameters.AddWithValue("@CreatedOn", frozenNow.ToString("o"));
                command.Parameters.AddWithValue("@CloudTransferBatchId", batchId);
                command.Parameters.AddWithValue("@BucketName", changes.AccountInformation.BucketName());
                command.Parameters.AddWithValue("@FileSystemFile", file.LocalFile.LocalFile.FullName);
                command.Parameters.AddWithValue("@FileSize", file.LocalFile.LocalFile.Length);
                command.Parameters.AddWithValue("@ExistingCloudObjectKey", file.ExistingRemoteFile.Key);
                command.Parameters.AddWithValue("@NewCloudObjectKey", file.LocalFile.CloudKey);
                command.Parameters.AddWithValue("@LastUpdatedOn", frozenNow.ToString("o"));
                command.Parameters.AddWithValue("@CopyCompletedSuccessfully", false);
                command.Parameters.AddWithValue("@ErrorMessage", string.Empty);
                
                await command.ExecuteNonQueryAsync();
            }
        }
        
        progress.Report($"Adding {changes.FileSystemFilesToUpload.Count} Files to Upload");
        
        await using (var command = new SQLiteCommand(connection))
        {
            command.CommandText =
                "INSERT INTO CloudUploads (BucketName, CloudObjectKey, CloudTransferBatchId, CreatedOn, ErrorMessage, FileSize, FileSystemFile, LastUpdatedOn, UploadCompletedSuccessfully) VALUES (@BucketName, @CloudObjectKey, @CloudTransferBatchId, @CreatedOn, @ErrorMessage, @FileSize, @FileSystemFile, @LastUpdatedOn, @UploadCompletedSuccessfully)";
            command.Prepare();
            
            foreach (var file in changes.FileSystemFilesToUpload)
            {
                command.Parameters.AddWithValue("@CreatedOn", frozenNow.ToString("o"));
                command.Parameters.AddWithValue("@CloudTransferBatchId", batchId);
                command.Parameters.AddWithValue("@BucketName", changes.AccountInformation.BucketName());
                command.Parameters.AddWithValue("@FileSystemFile", file.LocalFile.FullName);
                command.Parameters.AddWithValue("@FileSize", file.LocalFile.Length);
                command.Parameters.AddWithValue("@CloudObjectKey", file.CloudKey);
                command.Parameters.AddWithValue("@LastUpdatedOn", frozenNow.ToString("o"));
                command.Parameters.AddWithValue("@ErrorMessage", string.Empty);
                command.Parameters.AddWithValue("@UploadCompletedSuccessfully", false);
                
                await command.ExecuteNonQueryAsync();
            }
        }
        
        progress.Report($"Adding {changes.S3FilesToDelete.Count} S3 Files to Delete");
        
        await using (var command = new SQLiteCommand(connection))
        {
            command.CommandText =
                "INSERT INTO CloudDeletions (BucketName, CloudObjectKey, CloudTransferBatchId, CreatedOn, DeletionCompletedSuccessfully, ErrorMessage, FileSize, LastUpdatedOn) VALUES (@BucketName, @CloudObjectKey, @CloudTransferBatchId, @CreatedOn, @DeletionCompletedSuccessfully, @ErrorMessage, @FileSize, @LastUpdatedOn)";
            command.Prepare();
            
            foreach (var file in changes.S3FilesToDelete)
            {
                command.Parameters.AddWithValue("@CreatedOn", frozenNow.ToString("o"));
                command.Parameters.AddWithValue("@CloudTransferBatchId", batchId);
                command.Parameters.AddWithValue("@BucketName", changes.AccountInformation.BucketName());
                command.Parameters.AddWithValue("@CloudObjectKey", file.Key);
                command.Parameters.AddWithValue("@FileSize", file.Metadata.FileSize);
                command.Parameters.AddWithValue("@LastUpdatedOn", frozenNow.ToString("o"));
                command.Parameters.AddWithValue("@DeletionCompletedSuccessfully", false);
                command.Parameters.AddWithValue("@ErrorMessage", string.Empty);
                
                await command.ExecuteNonQueryAsync();
            }
        }
        
        progress.Report("Committing all changes...");
        
        transaction.Commit();
        connection.Close();
        
        var batchInformation = await CloudTransferBatchInformation.CreateInstance(batchId);

        Log.Information(
            "New Batch Created - Id {batchId}, New Cloud Scan {newCloudScan}, {uploadCount} Uploads, {deleteCount} Deletes, {localFilesCount} Local Files, {cloudFilesCount}",
            batchInformation.Batch.Id, batchInformation.Batch.BasedOnNewCloudFileScan, batchInformation.CloudUploads.Count,
            batchInformation.CloudDeletions.Count,
            batchInformation.FileSystemFiles.Count, batchInformation.CloudFiles.Count);
        
        DataNotifications.PublishDataNotification(nameof(CreationTools), DataNotificationContentType.CloudTransferBatch,
            DataNotificationUpdateType.New, changes.Job.PersistentId, batchInformation.Batch.Id);
        
        return batchInformation;
    }
}