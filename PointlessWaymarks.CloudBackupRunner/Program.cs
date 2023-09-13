using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Batch;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CloudBackupData.Reports;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;
using Serilog;

namespace PointlessWaymarks.CloudBackupRunner;

public static class Program
{
    public static async Task Main(string[] args)
    {
        LogTools.StandardStaticLoggerForProgramDirectory("CloudBackupRunner");

        AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs eventArgs)
        {
            var exceptionMessage = "Unhandled Fatal Exception";
            if (eventArgs.ExceptionObject is Exception castException) exceptionMessage = castException.Message;

            Log.ForContext("exceptionObject", eventArgs.ExceptionObject.SafeObjectDump())
                .Error(exceptionMessage, eventArgs.ExceptionObject);

            try
            {
                WindowsNotificationBuilders.NewNotifier("Cloud Backup Runner").Result
                    .SetAutomationLogoNotificationIconUrl().SetErrorReportAdditionalInformationMarkdown(
                        FileAndFolderTools.ReadAllText(
                            Path.Combine(AppContext.BaseDirectory, "README_CloudBackupRunner.md"))).Error("Unhandled Exception...")
                    .RunSynchronously();
            }
            catch (Exception)
            {
                // ignored
            }

            Log.CloseAndFlush();
        };

        var startTime = DateTime.Now;

        if (args.Length is < 1 or > 3)
        {
            Console.WriteLine("""
                              The PointlessWaymarks CloudBackup Runner uses Backup Jobs created with the
                              Pointless Waymarks Cloud Backup Editor to perform Uploads and Deletions on
                              Amazon S3 to create a backup of your local files. Backup Jobs are stored in
                              a Database File that is specified as the first argument to the program.
                              """);
            Console.WriteLine();
            Console.WriteLine("""
                              To list Backup Jobs and recent Transfer Batches specify the Database File
                              as the only argument to the program.
                              """);
            Console.WriteLine();
            Console.WriteLine("""
                              To run a Backup Job provide the Database File and Job Id as arguments.
                              """);
            Console.WriteLine();
            Console.WriteLine("""
                              By default when you run a Backup Job the program will scan every local and
                              S3 file for changes to create a 'Transfer Batch' in the database. The Transfer
                              Batch holds a record of all the Uploads and Deletions needed to
                              make S3 match your local files. With larger numbers of files this can take
                              a long time... If you have a large number of files, or a backup of files that
                              only change infrequently, it may make sense to resume a previous Batch.
                              """);
            Console.WriteLine();
            Console.WriteLine("""
                              Resuming a previous Transfer Batch will mean that the backup will NOT account
                              for any file changes since the Batch was created!! It will also mean the program
                              will spend more time uploading and deleting files and less time scanning for
                              changes... Use with caution!
                              """);
            Console.WriteLine();
            Console.WriteLine("""
                              To try to resume a batch specify the Database File, Job Id and:
                              """);
            Console.WriteLine();
            Console.WriteLine("""
                                Batch Id - To resume a specific Batch specify the Batch Id. If the Batch Id
                                is not found a new Batch will be created.
                              """);
            Console.WriteLine();
            Console.WriteLine("""
                                last - To resume the last Batch specify 'last'. If there is no last batch a
                                new Batch will be created.
                              """);
            Console.WriteLine();
            Console.WriteLine("""
                                auto - To have the program guess whether there is a batch worth resuming specify
                                'auto'. This will look for a recent batch that has a low error rate and still
                                needs a large number of uploads to complete. If a 'best guess' Batch is not found
                                a new batch will be created.
                              """);

            return;
        }

        var consoleId = Guid.NewGuid();

        var progress = new ConsoleAndDataNotificationProgress(consoleId);

        var errorNotifier = (await WindowsNotificationBuilders.NewNotifier("Cloud Backup Runner"))
            .SetAutomationLogoNotificationIconUrl().SetErrorReportAdditionalInformationMarkdown(
                FileAndFolderTools.ReadAllText(
                    Path.Combine(AppContext.BaseDirectory, "README_CloudBackupRunner.md")));

        Log.ForContext("args", args, true).Information(
            "PointlessWaymarks.CloudBackupRunner Starting");

        var db = await CloudBackupContext.TryCreateInstance(args[0]);

        if (!db.success || db.context is null)
        {
            await errorNotifier.Error($"Failed to Connect to the Db {args[0]}");

            Log.ForContext(nameof(db), db, true).Error("Failed to Connect to the Db {dbFile}", args[0]);
            Log.Information("Cloud Backup Runner - Finished Run");
            await Log.CloseAndFlushAsync();
            return;
        }

        if (args.Length == 1)
        {
            var jobs = await db.context.BackupJobs.ToListAsync();

            Log.Verbose("Found {jobCount} Jobs", jobs.Count);

            foreach (var loopJob in jobs)
            {
                Console.WriteLine(
                    $"{loopJob.Id}  {loopJob.Name}: {loopJob.LocalDirectory} to {loopJob.CloudBucket}:{loopJob.CloudDirectory}");

                var batches = loopJob.Batches.OrderByDescending(x => x.CreatedOn).Take(5).ToList();

                foreach (var loopBatch in batches)
                    Console.WriteLine(
                        $"  Batch Id {loopBatch.Id} - {loopBatch.CreatedOn} - Uploads {loopBatch.CloudUploads.Count(x => x.UploadCompletedSuccessfully)} of {loopBatch.CloudUploads.Count} Complete, Deletions {loopBatch.CloudDeletions.Count(x => x.DeletionCompletedSuccessfully)} of {loopBatch.CloudDeletions.Count} Complete, {loopBatch.CloudUploads.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage)) + loopBatch.CloudDeletions.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))} Errors");
            }

            Log.Information("Cloud Backup Runner - Finished Run");

            await Log.CloseAndFlushAsync();
            return;
        }

        if (!int.TryParse(args[1], out var jobId))
        {
            await errorNotifier.Error($"Failed to Parse Job Id {args[1]}");

            Log.Error("Failed to Parse Job Id {jobId}", args[1]);
            Log.Information("Cloud Backup Runner - Finished Run");
            await Log.CloseAndFlushAsync();
            return;
        }

        var backupJob = await db.context.BackupJobs.SingleOrDefaultAsync(x => x.Id == jobId);

        if (backupJob == null)
        {
            await errorNotifier.Error($"Failed to find a Backup Job with Id {jobId} in {args[0]}");

            Log.Error("Failed to find a Backup Job with Id {jobId} in {dbFile}", jobId, args[0]);
            Log.Information("Cloud Backup Runner - Finished Run");
            await Log.CloseAndFlushAsync();
            return;
        }

        progress.PersistentId = backupJob.PersistentId;

        Log.Logger = new LoggerConfiguration().StandardEnrichers().LogToConsole()
            .LogToFileInProgramDirectory("CloudBackupRunner").WriteTo.DelegatingTextSink(
                x => DataNotifications.PublishProgressNotification(consoleId.ToString(), x, backupJob.PersistentId),
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var cloudCredentials = PasswordVaultTools.GetCredentials(backupJob.VaultIdentifier);

        if (string.IsNullOrWhiteSpace(cloudCredentials.username) ||
            string.IsNullOrWhiteSpace(cloudCredentials.password))
        {
            await errorNotifier.Error($"Cloud Credentials are not Valid?");

            Log.Error(
                $"Cloud Credentials are not Valid? Access Key is blank {string.IsNullOrWhiteSpace(cloudCredentials.username)}, Password is blank {string.IsNullOrWhiteSpace(cloudCredentials.password)}");
            Log.Information("Cloud Backup Runner - Finished Run");
            await Log.CloseAndFlushAsync();
            return;
        }

        var frozenNow = DateTime.Now;

        var amazonCredentials = new S3AccountInformation
        {
            AccessKey = () => cloudCredentials.username,
            Secret = () => cloudCredentials.password,
            BucketName = () => backupJob.CloudBucket,
            BucketRegion = () => backupJob.CloudRegion,
            FullFileNameForJsonUploadInformation = () =>
                Path.Combine(FileLocationHelpers.DefaultStorageDirectory().FullName,
                    $"{frozenNow:yyyy-MM-dd-HH-mm}-{args[0]}.json"),
            FullFileNameForToExcel = () => Path.Combine(FileLocationHelpers.DefaultStorageDirectory().FullName,
                $"{frozenNow:yyyy-MM-dd-HH-mm}-{args[0]}.xlsx")
        };
        CloudTransferBatch? batch = null;

        var mostRecentCloudScanBatch =
            backupJob.Batches.Where(x => x.BasedOnNewCloudFileScan).MaxBy(x => x.CreatedOn);

//3 Args mean that a batch has been specified in one of 3 ways: auto, last, or id. On all of these options
//a 'bad' option (last when there is no last batch, id that doesn't match anything in the db...) will fall
//thru to a new batch being generated. 
        if (args.Length == 3)
        {
            //Auto: Very simple - if there is a batch in the last two weeks that is < 95% done and < 10% errors use it
            if (args[2].Equals("auto", StringComparison.OrdinalIgnoreCase) &&
                backupJob.Batches.Any(x => x.CreatedOn > DateTime.Now.AddDays(-14)))
            {
                var mostRecentBatch = backupJob.Batches.MaxBy(x => x.CreatedOn)!;
                var totalActions = mostRecentBatch.CloudUploads.Count + mostRecentBatch.CloudDeletions.Count;
                var successfulActions = mostRecentBatch.CloudUploads.Count(x => x.UploadCompletedSuccessfully) +
                                        mostRecentBatch.CloudDeletions.Count(x => x.DeletionCompletedSuccessfully);
                var errorActions =
                    mostRecentBatch.CloudUploads.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage)) +
                    mostRecentBatch.CloudDeletions.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage));
                var highPercentSuccess = successfulActions / (decimal)totalActions > .95M;
                var highPercentErrors = errorActions / (decimal)totalActions > .10M;

                Console.WriteLine("Auto Batch Selection");
                Console.WriteLine($"  Last Batch: Id {mostRecentBatch.Id} Created On: {mostRecentBatch.CreatedOn}");
                Console.WriteLine($"  Total Actions: {totalActions}");
                Console.WriteLine(
                    $"  Successful Actions: {successfulActions} - {successfulActions / (decimal)totalActions:P0}");
                Console.WriteLine($"  Error Actions: {errorActions} - {errorActions / (decimal)totalActions:P0}");
                Console.WriteLine($"    High Percent Success: {highPercentSuccess}");
                Console.WriteLine($"    High Percent Errors: {highPercentErrors}");

                //Case: We have a batch that seems likely to be productive to resume
                if (!highPercentSuccess && !highPercentErrors)
                {
                    batch = mostRecentBatch;
                    Log.ForContext(nameof(batch), batch.SafeObjectDumpNoEnumerables())
                        .Information("Batch Set to Batch Id {batchId} Based on auto argument and High Percent Success {highPercentSuccess} and High Percent Errors {highPercentErrors}", batch.Id, highPercentSuccess, highPercentErrors);
                }
                //Case: In the last 4 weeks there is a batch and at least one of the batches in the last 4 weeks is
                //based on a full cloud scan -> check for changes against the cache and if none are found then exit.
                else if (mostRecentBatch.CreatedOn > DateTime.Now.AddDays(-28) &&
                         mostRecentCloudScanBatch != null &&
                         mostRecentCloudScanBatch.CreatedOn > DateTime.Now.AddDays(-28))
                {
                    var possibleChanges =
                        await CreationTools.GetChangesBasedOnCloudCacheFilesAndLocalScan(amazonCredentials,
                            backupJob.Id,
                            progress);

                    if (!possibleChanges.FileSystemFilesToUpload.Any() && !possibleChanges.S3FilesToDelete.Any())
                    {
                        Log.ForContext(nameof(batch), batch.SafeObjectDump())
                            .ForContext(nameof(backupJob), backupJob.Dump())
                            .Information(
                                "Comparing Batch Id {batchId}'s Cloud Files (most recent batch, within the last 28 days) to the Cloud File Cache Files produced no backup actions - Nothing To Do, Stopping",
                                mostRecentBatch.Id);
                        Log.Information("Cloud Backup Runner - Finished Run");
                        return;
                    }
                }
                //Other cases fall thru to the 'null batch' logic below
            }
            //Last
            else if (args[2].Equals("last", StringComparison.OrdinalIgnoreCase))
            {
                batch = backupJob.Batches.MaxBy(x => x.CreatedOn);
                if (batch != null)
                    Log.ForContext(nameof(batch), batch.SafeObjectDumpNoEnumerables())
                        .Information("Batch Set to Batch Id {batchId} Based on last argument", batch.Id);
                else
                    Log.ForContext(nameof(batch), batch.SafeObjectDumpNoEnumerables())
                        .Information("Setting Batch based on last argument failed");
            }
            //Id
            else if (int.TryParse(args[2], out var parsedBatchId))
            {
                batch = backupJob.Batches.SingleOrDefault(x => x.Id == parsedBatchId);
                if (batch != null)
                    Log.ForContext(nameof(batch), batch.SafeObjectDumpNoEnumerables())
                        .Information("Batch Set to Batch Id {batchId} Based on Id", batch.Id);
                else
                    Log.ForContext(nameof(batch), batch.SafeObjectDumpNoEnumerables())
                        .Information("Setting Batch based on argument Id Argument {batchArgument} Failed", args[2]);
            }
        }

//Batch equals null here means either that no batch was specified or that the batch specification
//didn't return anything - either way a new batch is created, the source of the batch changes depends
//on the age of the last Cloud File Scan.
        if (batch == null)
        {
            if (mostRecentCloudScanBatch != null && mostRecentCloudScanBatch.CreatedOn > DateTime.Now.AddDays(-28))
            {
                batch = await CloudTransfer.CreateBatchInDatabaseFromCloudAndLocalScan(amazonCredentials, backupJob,
                    progress);
            }
            else
            {
                batch = await CloudTransfer.CreateBatchInDatabaseFromCloudAndLocalScan(amazonCredentials, backupJob,
                    progress);
            }
        }

        Log.ForContext(nameof(batch), batch.SafeObjectDumpNoEnumerables()).Information(
            "Using Batch Id {batchId} with {uploadCount} Uploads and {deleteCount} Deletes", batch.Id,
            batch.CloudUploads.Count, batch.CloudDeletions.Count);

        if (batch.CloudUploads.Count < 1 && batch.CloudDeletions.Count < 1)
        {
            Log.Information("Cloud Backup Ending - No Uploads or Deletions for Job Id {jobId} batch {batchId}",
                backupJob.Id,
                batch.Id);
            Log.Information("Cloud Backup Runner - Finished Run");
            await Log.CloseAndFlushAsync();
            return;
        }

        try
        {
            var runInformation =
                await CloudTransfer.CloudUploadAndDelete(amazonCredentials, batch.Id, startTime, progress);
            Log.ForContext(nameof(runInformation), runInformation, true).Information("Cloud Backup Ending");

            var batchReport = await BatchReportToExcel.Run(batch.Id, progress);

            (await WindowsNotificationBuilders.NewNotifier("Cloud Backup Runner"))
                .SetAutomationLogoNotificationIconUrl().MessageWithFile(
                    $"Uploaded {FileAndFolderTools.GetBytesReadable(runInformation.UploadedSize)} in {(runInformation.Ended - runInformation.Started).TotalHours:N2} Hours{(runInformation.DeleteErrorCount + runInformation.UploadErrorCount > 0 ? $" - {runInformation.DeleteErrorCount + runInformation.UploadErrorCount}  Errors" : string.Empty)} - Click for Report",
                    batchReport);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error Running Program...");
            Console.WriteLine(e);

            await (await WindowsNotificationBuilders.NewNotifier("Cloud Backup Runner"))
                .SetAutomationLogoNotificationIconUrl().SetErrorReportAdditionalInformationMarkdown(
                    FileAndFolderTools.ReadAllText(
                        Path.Combine(AppContext.BaseDirectory, "README_CloudBackupRunner.md"))).Error(e);
        }
        finally
        {
            Log.Information("Cloud Backup Runner - Finished Run");
            await Log.CloseAndFlushAsync();
        }
    }
}