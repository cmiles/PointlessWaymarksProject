﻿using Microsoft.EntityFrameworkCore;
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
            CloudDirectory = "Test01"
        };

        db.BackupJob.Add(testJob);

        await db.SaveChangesAsync();
    }

    [Test]
    public async Task T000_BasicIntegrationNoExclusionsPatternNoWildcard()
    {
        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJob.SingleAsync();
        var files = await CreationTools.GetAllLocalFiles(job);

        Assert.That(files, Has.Count.EqualTo(8));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile1.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile2.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile3.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile3Duplicate.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile4.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile5.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile6.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile7.FullName));

        //Sanity Check the MD5 Hashes and LastWriteTimes - the TestFile3Duplicate file should helps this
        //have meaning since it should have the same hash and last write time as TestFile3
        Assert.That(files.Select(x => x.Metadata.FileSystemHash).Distinct().ToList(), Has.Count.EqualTo(7));
        Assert.That(files.Select(x => x.Metadata.LastWriteTime).Distinct().ToList(), Has.Count.EqualTo(7));
    }

    [Test]
    public async Task T100_PatternNoWildcard()
    {
        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJob.SingleAsync();

        context.ExcludedFileNamePatterns.Add(new ExcludedFileNamePattern
        {
            CreatedOn = DateTime.Now,
            JobId = job.Id,
            Pattern = TestFile5.Name
        });
        await context.SaveChangesAsync();

        var files = await CreationTools.GetAllLocalFiles(job);

        Assert.That(files, Has.Count.EqualTo(7));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile1.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile2.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile3.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile3Duplicate.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile4.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile6.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile7.FullName));

        //Sanity Check the MD5 Hashes and LastWriteTimes - the TestFile3Duplicate file should helps this
        //have meaning since it should have the same hash and last write time as TestFile3
        Assert.That(files.Select(x => x.Metadata.FileSystemHash).Distinct().ToList(), Has.Count.EqualTo(6));
        Assert.That(files.Select(x => x.Metadata.LastWriteTime).Distinct().ToList(), Has.Count.EqualTo(6));
    }


    [Test]
    public async Task T110_PatternStarWildcardExtension()
    {
        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJob.SingleAsync();

        context.ExcludedFileNamePatterns.Add(new ExcludedFileNamePattern
        {
            CreatedOn = DateTime.Now,
            JobId = job.Id,
            Pattern = "*.txt"
        });
        await context.SaveChangesAsync();

        var files = await CreationTools.GetAllLocalFiles(job);

        Assert.That(files, Has.Count.EqualTo(5));

        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile3.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile3Duplicate.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile4.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile6.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile7.FullName));

        //Sanity Check the MD5 Hashes and LastWriteTimes - the TestFile3Duplicate file should helps this
        //have meaning since it should have the same hash and last write time as TestFile3
        Assert.That(files.Select(x => x.Metadata.FileSystemHash).Distinct().ToList(), Has.Count.EqualTo(4));
        Assert.That(files.Select(x => x.Metadata.LastWriteTime).Distinct().ToList(), Has.Count.EqualTo(4));
    }

    [Test]
    public async Task T120_PatternStarFileName()
    {
        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJob.SingleAsync();

        context.ExcludedFileNamePatterns.Add(new ExcludedFileNamePattern
        {
            CreatedOn = DateTime.Now,
            JobId = job.Id,
            Pattern = "*3*"
        });
        await context.SaveChangesAsync();

        var files = await CreationTools.GetAllLocalFiles(job);

        Assert.That(files, Has.Count.EqualTo(2));

        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile6.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile7.FullName));

        //Sanity Check the MD5 Hashes and LastWriteTimes - the TestFile3Duplicate file should helps this
        //have meaning since it should have the same hash and last write time as TestFile3
        Assert.That(files.Select(x => x.Metadata.FileSystemHash).Distinct().ToList(), Has.Count.EqualTo(2));
        Assert.That(files.Select(x => x.Metadata.LastWriteTime).Distinct().ToList(), Has.Count.EqualTo(2));
    }

    [Test]
    public async Task T200_DirectoryExclusion()
    {
        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJob.SingleAsync();
        context.ExcludedFileNamePatterns.RemoveRange(context.ExcludedFileNamePatterns);

        context.ExcludedDirectories.Add(new ExcludedDirectory
        {
            CreatedOn = DateTime.Now,
            JobId = job.Id,
            Directory = TestDirectory1.FullName
        });
        await context.SaveChangesAsync();

        var files = await CreationTools.GetAllLocalFiles(job);

        Assert.That(files, Has.Count.EqualTo(3));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile5.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile6.FullName));
        Assert.That(files.Select(x => x.LocalFile.FullName), Has.Exactly(1).EqualTo(TestFile7.FullName));

        //Sanity Check the MD5 Hashes and LastWriteTimes - the TestFile3Duplicate file should helps this
        //have meaning since it should have the same hash and last write time as TestFile3
        Assert.That(files.Select(x => x.Metadata.FileSystemHash).Distinct().ToList(), Has.Count.EqualTo(3));
        Assert.That(files.Select(x => x.Metadata.LastWriteTime).Distinct().ToList(), Has.Count.EqualTo(3));
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