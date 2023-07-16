using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Batch;
using PointlessWaymarks.CloudBackupRunner;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;
using Serilog;

if (args.Length is < 1 or > 2)
{
    Console.WriteLine("The PointlessWaymarks CloudBackup Runner uses Jobs in an ");
    Console.WriteLine("existing data to run a backup. See the Pointless Waymarks ");
    Console.WriteLine("Cloud Backup Gui project to create backup Jobs.");
    Console.WriteLine();
    Console.WriteLine("To list the Jobs in a Database Specify the Filename");
    Console.WriteLine("To run a job specify the Database name and Job Id");
}

var consoleId = Guid.NewGuid();

var progress = new ConsoleAndDataNotificationProgress(consoleId);

LogTools.StandardStaticLoggerForProgramDirectory("PhotoPickup");

Log.ForContext("args", args, true).Information(
    "PointlessWaymarks.CloudBackupRunner Starting");

var db = await CloudBackupContext.TryCreateInstance(args[0]);

if (!db.success || db.context is null)
{
    Log.ForContext(nameof(db), db, true).Error("Failed to Connect to the Db {dbFile}", args[0]);
    Log.CloseAndFlush();
    return;
}

if (args.Length == 1)
{
    var jobs = await db.context.BackupJobs.ToListAsync();

    Log.Verbose("Found {jobCount} Jobs", jobs.Count);

    foreach (var loopJob in jobs)
        Console.WriteLine(
            $"{loopJob.Id}  {loopJob.Name}: {loopJob.LocalDirectory} to {loopJob.CloudBucket}:{loopJob.CloudDirectory}");

    Log.CloseAndFlush();
    return;
}

if (!int.TryParse(args[1], out var jobId))
{
    Log.Error("Failed to Parse Job Id {jobId}", args[0]);
    Log.CloseAndFlush();
    return;
}

var backupJob = await db.context.BackupJobs.SingleOrDefaultAsync(x => x.Id == jobId);

if (backupJob == null)
{
    Log.Error("Failed to find a Backup Job with Id {jobId} in {dbFile}", jobId, args[0]);
    Log.CloseAndFlush();
    return;
}

progress.PersistentId = backupJob.PersistentId;

var cloudCredentials = PasswordVaultTools.GetCredentials(backupJob.VaultIdentifier);

if (string.IsNullOrWhiteSpace(cloudCredentials.username) || string.IsNullOrWhiteSpace(cloudCredentials.password))
{
    Log.Error(
        $"Cloud Credentials are not Valid? Access Key is blank {string.IsNullOrWhiteSpace(cloudCredentials.username)}, Password is blank {string.IsNullOrWhiteSpace(cloudCredentials.password)}");
    Log.CloseAndFlush();
    return;
}

var frozenNow = DateTime.Now;

var amazonCredentials = new S3AccountInformation
{
    AccessKey = () => cloudCredentials.username,
    Secret = () => cloudCredentials.password,
    BucketName = () => backupJob.CloudBucket,
    BucketRegion = () => backupJob.CloudRegion,
    FullFileNameForJsonUploadInformation = () => Path.Combine(FileLocationHelpers.DefaultStorageDirectory().FullName,
        $"{frozenNow:yyyy-MM-dd-HH-mm}-{args[0]}.json"),
    FullFileNameForToExcel = () => Path.Combine(FileLocationHelpers.DefaultStorageDirectory().FullName,
        $"{frozenNow:yyyy-MM-dd-HH-mm}-{args[0]}.xlsx")
};

var batch = await CloudTransfer.CreateBatchInDatabaseFromChanges(amazonCredentials, backupJob, progress);

Log.Information("Created Batch Id {batchId} with {uploadCount} Uploads and {deleteCount} Deletes", batch.Id,
    batch.CloudUploads.Count, batch.CloudDeletions.Count);

if (batch.CloudUploads.Count < 1 && batch.CloudDeletions.Count < 1)
{
    Log.Information("Cloud Backup Ending - No Uploads or Deletions for Job Id {jobId} batch {batchId}", backupJob.Id,
        batch.Id);
    Log.CloseAndFlush();
    return;
}

try
{
    var runInformation = await CloudTransfer.CloudUploadAndDelete(amazonCredentials, batch.Id, progress);
    Log.ForContext(nameof(runInformation), runInformation, true).Information("Cloud Backup Ending");

    (await WindowsNotificationBuilders.NewNotifier("Cloud Backup Runner"))
        .SetAutomationLogoNotificationIconUrl().Message(
            $"Uploaded {FileAndFolderTools.GetBytesReadable(runInformation.UploadedSize)} in {(runInformation.Ended - runInformation.Started).TotalHours:N2} Hours{((runInformation.DeleteErrorCount + runInformation.UploadErrorCount) > 0 ? $"{runInformation.DeleteErrorCount + runInformation.UploadErrorCount} Errors" : string.Empty)}");
}
catch (Exception e)
{
    Log.Error(e, "Error Running Program...");
    Console.WriteLine(e);

    await (await WindowsNotificationBuilders.NewNotifier("Cloud Backup Runner"))
        .SetAutomationLogoNotificationIconUrl().SetErrorReportAdditionalInformationMarkdown(
            FileAndFolderTools.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "README.md"))).Error(e);
}
finally
{
    Log.CloseAndFlush();
}