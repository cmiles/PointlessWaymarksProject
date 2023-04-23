using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Batch;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CloudBackupTests;

/// <summary>
///     This tests both database and S3 listings/uploads. The intent is that this is used with a 'local' S3
///     service such as:
///     [GitHub - adobe/S3Mock: A simple mock implementation of the AWS S3 API
///     ](https://github.com/adobe/S3Mock#configuration)
/// </summary>
public class DatabaseBatchesAndUploadSeries
{
    public IS3AccountInformation S3Credentials { get; set; }

    /// <summary>
    ///     Setup a test directory with a subdirectory structure, test database and a test job.
    /// </summary>
    /// <returns></returns>
    [OneTimeSetUp]
    public async Task Setup()
    {
        var storageDirectory = FileLocationTools.DefaultStorageDirectory();

        var testTime = $"{DateTime.Now:yyyy-MM-dd-hh-mm-ss-ff}";

        DbDirectory = storageDirectory.CreateSubdirectory($"TempCloudBackupTest-{testTime}");

        TestDirectory = DbDirectory.CreateSubdirectory("FileTest");

        TestDirectory1 = TestDirectory.CreateSubdirectory("TestL");
        TestFile1 = Helpers.RandomFile(Path.Combine(TestDirectory1.FullName, "TestFile1.txt"));
        await Task.Delay(100);

        TestFile2 = Helpers.RandomFile(Path.Combine(TestDirectory1.FullName, "TestFile2.txt"));
        await Task.Delay(100);

        TestFile3 = Helpers.RandomFile(Path.Combine(TestDirectory1.FullName, "TestFile3.doc"));
        TestFile3Duplicate = TestFile3.CopyTo(Path.Combine(TestDirectory1.FullName, "TestFile3x.doc"));
        await Task.Delay(100);

        TestFile4 = Helpers.RandomFile(Path.Combine(TestDirectory1.FullName, "fake.123"));
        await Task.Delay(100);

        TestDirectory2 = TestDirectory.CreateSubdirectory("TestR");
        TestFile5 = Helpers.RandomFile(Path.Combine(TestDirectory2.FullName, "TestFile.txt"));
        await Task.Delay(100);

        TestFile6 = Helpers.RandomFile(Path.Combine(TestDirectory2.FullName, "TestFile.lhk"));
        await Task.Delay(100);

        TestDirectory3 = TestDirectory2.CreateSubdirectory("R1");
        TestFile7 = Helpers.RandomFile(Path.Combine(TestDirectory2.FullName, "with space.JSON"));
        await Task.Delay(100);

        var testDb = Path.Combine(DbDirectory.FullName, "TestDb.db");

        var db = await CloudBackupContext.CreateInstanceWithEnsureCreated(testDb);

        var testJob = new BackupJob
        {
            CreatedOn = DateTime.Now,
            LocalDirectory = TestDirectory.FullName,
            CloudDirectory = $"Cloud-{testTime}",
            Name = $"Cloud-{testTime}"
        };

        db.BackupJob.Add(testJob);

        await db.SaveChangesAsync();

        S3Credentials = new S3LocalAccountInformation
            { LocalStackBucket = $"b{DateTime.Now:yyyyMMddhhmmssfff}.pointlesswaymarks.com" };
        var localStockClient = S3Credentials.S3Client();

        await localStockClient.PutBucketAsync(new PutBucketRequest
        {
            BucketName = S3Credentials.BucketName(),
            UseClientRegion = true
        });
    }

    [Test]
    public async Task T000_InitialTransferAndFileCheck()
    {
        var job = await (await CloudBackupContext.CreateInstance()).BackupJob.SingleAsync();
        var batch = await CreateCloudTransferBatch.InDatabase(S3Credentials, job);

        var testBatch =
            await (await CloudBackupContext.CreateInstance()).CloudTransferBatches.SingleAsync(x => x.Id == batch.Id);

        Assert.That(testBatch.CloudUploads, Has.Count.EqualTo(8));
        Assert.That(testBatch.CloudDeletions, Has.Count.EqualTo(0));
        Assert.That(testBatch.FileSystemFiles, Has.Count.EqualTo(8));

        await TransferCloudTransferBatch.UploadsAndDeletes(S3Credentials, testBatch.Id, null);

        testBatch =
            await (await CloudBackupContext.CreateInstance()).CloudTransferBatches.SingleAsync(x => x.Id == batch.Id);

        Assert.That(testBatch.CloudUploads.Where(x => x.UploadCompletedSuccessfully).ToList(),
            Has.Count.EqualTo(8));
        Assert.That(testBatch.CloudDeletions.Where(x => x.DeletionCompletedSuccessfully).ToList(),
            Has.Count.EqualTo(0));

        var batchAfterUpload = await CreateCloudTransferBatch.InDatabase(S3Credentials, job);

        Assert.That(batchAfterUpload.CloudUploads, Has.Count.EqualTo(0));
        Assert.That(batchAfterUpload.CloudDeletions, Has.Count.EqualTo(0));
        Assert.That(batchAfterUpload.FileSystemFiles, Has.Count.EqualTo(8));
    }

    [Test]
    public async Task T010_SecondTransferWithNewFileCheck()
    {
        TestFile1 = Helpers.RandomFile(Path.Combine(TestDirectory1.FullName, "TestFile1.txt"));

        var job = await (await CloudBackupContext.CreateInstance()).BackupJob.SingleAsync();
        var batch = await CreateCloudTransferBatch.InDatabase(S3Credentials, job);

        var testBatch =
            await (await CloudBackupContext.CreateInstance()).CloudTransferBatches.SingleAsync(x => x.Id == batch.Id);

        Assert.That(testBatch.CloudUploads, Has.Count.EqualTo(1));
        Assert.That(testBatch.CloudDeletions, Has.Count.EqualTo(0));
        Assert.That(testBatch.FileSystemFiles, Has.Count.EqualTo(8));

        await TransferCloudTransferBatch.UploadsAndDeletes(S3Credentials, testBatch.Id, null);

        testBatch =
            await (await CloudBackupContext.CreateInstance()).CloudTransferBatches.SingleAsync(x => x.Id == batch.Id);

        Assert.That(testBatch.CloudUploads.Where(x => x.UploadCompletedSuccessfully).ToList(),
            Has.Count.EqualTo(1));
        Assert.That(testBatch.CloudDeletions.Where(x => x.DeletionCompletedSuccessfully).ToList(),
            Has.Count.EqualTo(0));

        var batchAfterUpload = await CreateCloudTransferBatch.InDatabase(S3Credentials, job);

        Assert.That(batchAfterUpload.CloudUploads, Has.Count.EqualTo(0));
        Assert.That(batchAfterUpload.CloudDeletions, Has.Count.EqualTo(0));
        Assert.That(batchAfterUpload.FileSystemFiles, Has.Count.EqualTo(8));
    }

    [Test]
    public async Task T020_ThirdTransferWithDeletedFile()
    {
        TestFile3Duplicate.Delete();

        var job = await (await CloudBackupContext.CreateInstance()).BackupJob.SingleAsync();
        var batch = await CreateCloudTransferBatch.InDatabase(S3Credentials, job);

        var testBatch =
            await (await CloudBackupContext.CreateInstance()).CloudTransferBatches.SingleAsync(x => x.Id == batch.Id);

        Assert.That(testBatch.CloudUploads, Has.Count.EqualTo(0));
        Assert.That(testBatch.CloudDeletions, Has.Count.EqualTo(1));
        Assert.That(testBatch.FileSystemFiles, Has.Count.EqualTo(7));

        await TransferCloudTransferBatch.UploadsAndDeletes(S3Credentials, testBatch.Id, null);

        testBatch =
            await (await CloudBackupContext.CreateInstance()).CloudTransferBatches.SingleAsync(x => x.Id == batch.Id);

        Assert.That(testBatch.CloudUploads.Where(x => x.UploadCompletedSuccessfully).ToList(),
            Has.Count.EqualTo(0));
        Assert.That(testBatch.CloudDeletions.Where(x => x.DeletionCompletedSuccessfully).ToList(),
            Has.Count.EqualTo(1));

        var batchAfterUpload = await CreateCloudTransferBatch.InDatabase(S3Credentials, job);

        Assert.That(batchAfterUpload.CloudUploads, Has.Count.EqualTo(0));
        Assert.That(batchAfterUpload.CloudDeletions, Has.Count.EqualTo(0));
        Assert.That(batchAfterUpload.FileSystemFiles, Has.Count.EqualTo(7));
    }

    [Test]
    public async Task T100_ThirdTransferWithAddedDirectoryExclusion()
    {
        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJob.SingleAsync();
        job.ExcludedDirectories.Add(new ExcludedDirectory
            { CreatedOn = DateTime.Now, Directory = TestDirectory1.FullName, Job = job });
        await context.SaveChangesAsync();

        var batch = await CreateCloudTransferBatch.InDatabase(S3Credentials, job);

        var testBatch =
            await (await CloudBackupContext.CreateInstance()).CloudTransferBatches.SingleAsync(x => x.Id == batch.Id);

        Assert.That(testBatch.CloudUploads, Has.Count.EqualTo(0));
        Assert.That(testBatch.CloudDeletions, Has.Count.EqualTo(4));
        Assert.That(testBatch.FileSystemFiles, Has.Count.EqualTo(3));

        await TransferCloudTransferBatch.UploadsAndDeletes(S3Credentials, testBatch.Id, null);

        testBatch =
            await (await CloudBackupContext.CreateInstance()).CloudTransferBatches.SingleAsync(x => x.Id == batch.Id);

        Assert.That(testBatch.CloudUploads.Where(x => x.UploadCompletedSuccessfully).ToList(),
            Has.Count.EqualTo(0));
        Assert.That(testBatch.CloudDeletions.Where(x => x.DeletionCompletedSuccessfully).ToList(),
            Has.Count.EqualTo(4));

        var batchAfterUpload = await CreateCloudTransferBatch.InDatabase(S3Credentials, job);

        Assert.That(batchAfterUpload.CloudUploads, Has.Count.EqualTo(0));
        Assert.That(batchAfterUpload.CloudDeletions, Has.Count.EqualTo(0));
        Assert.That(batchAfterUpload.FileSystemFiles, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task T200_FourthTransferWithAddsFromRemovingAddedDirectoryExclusionAndAdditionalDelete()
    {
        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJob.SingleAsync();
        job.ExcludedDirectories.Remove(job.ExcludedDirectories.First());
        await context.SaveChangesAsync();

        TestFile7.Delete();

        var batch = await CreateCloudTransferBatch.InDatabase(S3Credentials, job);

        var testBatch =
            await (await CloudBackupContext.CreateInstance()).CloudTransferBatches.SingleAsync(x => x.Id == batch.Id);

        Assert.That(testBatch.CloudUploads, Has.Count.EqualTo(4));
        Assert.That(testBatch.CloudDeletions, Has.Count.EqualTo(1));
        Assert.That(testBatch.FileSystemFiles, Has.Count.EqualTo(6));

        await TransferCloudTransferBatch.UploadsAndDeletes(S3Credentials, testBatch.Id, null);

        testBatch =
            await (await CloudBackupContext.CreateInstance()).CloudTransferBatches.SingleAsync(x => x.Id == batch.Id);

        Assert.That(testBatch.CloudUploads.Where(x => x.UploadCompletedSuccessfully).ToList(),
            Has.Count.EqualTo(4));
        Assert.That(testBatch.CloudDeletions.Where(x => x.DeletionCompletedSuccessfully).ToList(),
            Has.Count.EqualTo(1));

        var batchAfterUpload = await CreateCloudTransferBatch.InDatabase(S3Credentials, job);

        Assert.That(batchAfterUpload.CloudUploads, Has.Count.EqualTo(0));
        Assert.That(batchAfterUpload.CloudDeletions, Has.Count.EqualTo(0));
        Assert.That(batchAfterUpload.FileSystemFiles, Has.Count.EqualTo(6));
    }

    private void UploadRequestOnUploadProgressEvent(object? sender, UploadProgressArgs e)
    {
    }

#pragma warning disable CS8618
    public DirectoryInfo DbDirectory { get; set; }
    public DirectoryInfo TestDirectory { get; set; }
    public DirectoryInfo TestDirectory1 { get; set; }
    public DirectoryInfo TestDirectory2 { get; set; }
    public DirectoryInfo TestDirectory3 { get; set; }
    public FileInfo TestFile1 { get; set; }
    public FileInfo TestFile2 { get; set; }
    public FileInfo TestFile3 { get; set; }
    public FileInfo TestFile3Duplicate { get; set; }
    public FileInfo TestFile4 { get; set; }
    public FileInfo TestFile5 { get; set; }
    public FileInfo TestFile6 { get; set; }
    public FileInfo TestFile7 { get; set; }
#pragma warning restore CS8618
}