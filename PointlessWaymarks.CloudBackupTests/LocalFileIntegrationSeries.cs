using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Batch;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupTests;

/// <summary>
///     A series to verify the discovery of Local Files for a Backup Job including Exclusions.
///     These tests build on each other and are only designed to work if you run all tests
///     (they are named to run in order). The OneTimeSetup created test directory and artifacts are
///     NOT cleaned up to allows easy inspection without debugging/changes.
/// </summary>
public class LocalFileIntegrationSeries
{
    [OneTimeSetUp]
    public async Task Setup()
    {
        var storageDirectory = FileLocationTools.DefaultStorageDirectory();

        DbDirectory = storageDirectory.CreateSubdirectory($"TempCloudBackupTest-{DateTime.Now:yyyy-MM-dd-hh-mm-ss-ff}");

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
            CloudDirectory = "Test01",
            Name = "Test01",
            MaximumRunTimeInHours = 1
        };

        db.BackupJobs.Add(testJob);

        await db.SaveChangesAsync();
    }

    [Test]
    public async Task T000_BasicIntegrationNoExclusionsPatternNoWildcard()
    {
        var progress = new ConsoleProgress();

        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJobs.SingleAsync();
        var includedFiles = await CreationTools.GetIncludedLocalFiles(job.Id, progress);
        var excludedFiles = await CreationTools.GetExcludedLocalFiles(job.Id, progress);

        Assert.Multiple(() =>
        {
            Assert.That(includedFiles, Has.Count.EqualTo(8));
            Assert.That(excludedFiles, Has.Count.EqualTo(0));
        });
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile1.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile2.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile3.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile3Duplicate.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile4.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile5.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile6.FullName));
        Assert.Multiple(() =>
        {
            Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile7.FullName));

            //Sanity Check the MD5 Hashes and LastWriteTimes - the TestFile3Duplicate file should help this
            //have meaning since it should have the same hash and last write time as TestFile3
            Assert.That(includedFiles.Select(x => x.UploadMetadata.FileSystemHash).Distinct().ToList(), Has.Count.EqualTo(7));
            Assert.That(includedFiles.Select(x => x.UploadMetadata.LastWriteTime).Distinct().ToList(), Has.Count.EqualTo(7));
        });
    }

    [Test]
    public async Task T100_PatternNoWildcard()
    {
        var progress = new ConsoleProgress();

        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJobs.SingleAsync();

        context.ExcludedFileNamePatterns.Add(new ExcludedFileNamePattern
        {
            CreatedOn = DateTime.Now,
            BackupJobId = job.Id,
            Pattern = TestFile5.Name
        });
        await context.SaveChangesAsync();

        var includedFiles = await CreationTools.GetIncludedLocalFiles(job.Id, progress);
        var excludedFiles = await CreationTools.GetExcludedLocalFiles(job.Id, progress);

        Assert.Multiple(() =>
        {
            Assert.That(includedFiles, Has.Count.EqualTo(7));
            Assert.That(excludedFiles, Has.Count.EqualTo(1));
        });
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile1.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile2.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile3.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile3Duplicate.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile4.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile6.FullName));
        Assert.Multiple(() =>
        {
            Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile7.FullName));

            //Sanity Check the MD5 Hashes and LastWriteTimes - the TestFile3Duplicate file should help this
            //have meaning since it should have the same hash and last write time as TestFile3
            Assert.That(includedFiles.Select(x => x.UploadMetadata.FileSystemHash).Distinct().ToList(), Has.Count.EqualTo(6));
            Assert.That(includedFiles.Select(x => x.UploadMetadata.LastWriteTime).Distinct().ToList(), Has.Count.EqualTo(6));
        });
    }


    [Test]
    public async Task T110_PatternStarWildcardExtension()
    {
        var progress = new ConsoleProgress();

        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJobs.SingleAsync();

        context.ExcludedFileNamePatterns.Add(new ExcludedFileNamePattern
        {
            CreatedOn = DateTime.Now,
            BackupJobId = job.Id,
            Pattern = "*.txt"
        });
        await context.SaveChangesAsync();

        var includedFiles = await CreationTools.GetIncludedLocalFiles(job.Id, progress);
        var excludedFiles = await CreationTools.GetExcludedLocalFiles(job.Id, progress);

        Assert.Multiple(() =>
        {
            Assert.That(includedFiles, Has.Count.EqualTo(5));
            Assert.That(excludedFiles, Has.Count.EqualTo(3));
        });
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile3.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile3Duplicate.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile4.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile6.FullName));
        Assert.Multiple(() =>
        {
            Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile7.FullName));

            //Sanity Check the MD5 Hashes and LastWriteTimes - the TestFile3Duplicate file should help this
            //have meaning since it should have the same hash and last write time as TestFile3
            Assert.That(includedFiles.Select(x => x.UploadMetadata.FileSystemHash).Distinct().ToList(), Has.Count.EqualTo(4));
            Assert.That(includedFiles.Select(x => x.UploadMetadata.LastWriteTime).Distinct().ToList(), Has.Count.EqualTo(4));
        });
    }

    [Test]
    public async Task T120_PatternStarFileName()
    {
        var progress = new ConsoleProgress();

        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJobs.SingleAsync();

        context.ExcludedFileNamePatterns.Add(new ExcludedFileNamePattern
        {
            CreatedOn = DateTime.Now,
            BackupJobId = job.Id,
            Pattern = "*3*"
        });
        await context.SaveChangesAsync();

        var includedFiles = await CreationTools.GetIncludedLocalFiles(job.Id, progress);
        var excludedFiles = await CreationTools.GetExcludedLocalFiles(job.Id, progress);

        Assert.Multiple(() =>
        {
            Assert.That(includedFiles, Has.Count.EqualTo(2));
            Assert.That(excludedFiles, Has.Count.EqualTo(6));
        });
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile6.FullName));
        Assert.Multiple(() =>
        {
            Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile7.FullName));

            //Sanity Check the MD5 Hashes and LastWriteTimes - the TestFile3Duplicate file should help this
            //have meaning since it should have the same hash and last write time as TestFile3
            Assert.That(includedFiles.Select(x => x.UploadMetadata.FileSystemHash).Distinct().ToList(), Has.Count.EqualTo(2));
            Assert.That(includedFiles.Select(x => x.UploadMetadata.LastWriteTime).Distinct().ToList(), Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task T200_DirectoryExclusion()
    {
        var progress = new ConsoleProgress();

        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJobs.SingleAsync();
        context.ExcludedFileNamePatterns.RemoveRange(context.ExcludedFileNamePatterns);

        context.ExcludedDirectories.Add(new ExcludedDirectory
        {
            CreatedOn = DateTime.Now,
            BackupJobId = job.Id,
            Directory = TestDirectory1.FullName
        });
        await context.SaveChangesAsync();

        var includedFiles = await CreationTools.GetIncludedLocalFiles(job.Id, progress);
        var excludedFiles = await CreationTools.GetExcludedLocalFiles(job.Id, progress);

        Assert.Multiple(() =>
        {
            Assert.That(includedFiles, Has.Count.EqualTo(3));
            Assert.That(excludedFiles, Has.Count.EqualTo(5));
        });
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile5.FullName));
        Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile6.FullName));
        Assert.Multiple(() =>
        {
            Assert.That(includedFiles.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile7.FullName));

            //Sanity Check the MD5 Hashes and LastWriteTimes - the TestFile3Duplicate file should help this
            //have meaning since it should have the same hash and last write time as TestFile3
            Assert.That(includedFiles.Select(x => x.UploadMetadata.FileSystemHash).Distinct().ToList(), Has.Count.EqualTo(3));
            Assert.That(includedFiles.Select(x => x.UploadMetadata.LastWriteTime).Distinct().ToList(), Has.Count.EqualTo(3));
        });
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