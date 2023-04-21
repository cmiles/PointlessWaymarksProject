﻿using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Batch;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CloudBackupTests;

/// <summary>
/// This tests both database and S3 listings/uploads. The intent is that this is used with a 'local' S3
/// service such as:
/// [GitHub - adobe/S3Mock: A simple mock implementation of the AWS S3 API ](https://github.com/adobe/S3Mock#configuration)
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
        TestFile2 = Helpers.RandomFile(Path.Combine(TestDirectory1.FullName, "TestFile2.txt"));
        TestFile3 = Helpers.RandomFile(Path.Combine(TestDirectory1.FullName, "TestFile3.doc"));
        TestFile3Duplicate = TestFile3.CopyTo(Path.Combine(TestDirectory1.FullName, "TestFile3x.doc"));
        TestFile4 = Helpers.RandomFile(Path.Combine(TestDirectory1.FullName, "fake.123"));

        TestDirectory2 = TestDirectory.CreateSubdirectory("TestR");
        TestFile5 = Helpers.RandomFile(Path.Combine(TestDirectory2.FullName, "TestFile.txt"));
        TestFile6 = Helpers.RandomFile(Path.Combine(TestDirectory2.FullName, "TestFile.lhk"));

        TestDirectory3 = TestDirectory2.CreateSubdirectory("R1");
        TestFile7 = Helpers.RandomFile(Path.Combine(TestDirectory2.FullName, "with space.JSON"));

        var testDb = Path.Combine(DbDirectory.FullName, "TestDb.db");

        var db = await CloudBackupContext.CreateInstanceWithEnsureCreated(testDb);

        var testJob = new BackupJob
        {
            CreatedOn = DateTime.Now,
            LocalDirectory = TestDirectory.FullName,
            CloudDirectory = $"Cloud-{testTime}"
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
        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJob.SingleAsync();
        var batch = await CreateCloudTransferBatch.InDatabase(S3Credentials, job);

        var testBatch = context.CloudTransferBatches.Include(x => x.FileSystemFiles).Include(x => x.CloudUploads)
            .Include(x => x.CloudDeletions).Single(x => x.Id == batch.Id);

        Assert.That(testBatch.CloudUploads, Has.Count.EqualTo(8));
        Assert.That(testBatch.CloudDeletions, Has.Count.EqualTo(0));
        Assert.That(testBatch.FileSystemFiles, Has.Count.EqualTo(8));

        var toUpload = testBatch.CloudUploads;

        var transferUtility = new TransferUtility(S3Credentials.S3Client());

        foreach (var loopUploads in toUpload)
        {
            var uploadRequest = await S3Tools.S3TransferUploadRequest(new FileInfo(loopUploads.FileSystemFile),
                S3Credentials.BucketName(), loopUploads.CloudObjectKey);
            uploadRequest.UploadProgressEvent += UploadRequestOnUploadProgressEvent;

            await transferUtility.UploadAsync(uploadRequest);
        }

        var batchAfterUpload = await CreateCloudTransferBatch.InDatabase(S3Credentials, job);

        Assert.That(batchAfterUpload.CloudUploads, Has.Count.EqualTo(0));
        Assert.That(batchAfterUpload.CloudDeletions, Has.Count.EqualTo(0));
        Assert.That(batchAfterUpload.FileSystemFiles, Has.Count.EqualTo(8));
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