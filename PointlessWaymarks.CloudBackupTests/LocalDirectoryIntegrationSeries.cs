using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Batch;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupTests;

/// <summary>
///     A series to verify the discovery of Local Directories for a Backup Job including Exclusions.
///     These tests build on each other and are only designed to work if you run all tests
///     (they are named to run in order). The OneTimeSetup created test directory and artifacts are
///     NOT cleaned up to allows easy inspection without debugging/changes.
/// </summary>
public class LocalDirectoryIntegrationSeries
{
    /// <summary>
    ///     Setup a test directory with a subdirectory structure, test database and a test job
    /// </summary>
    /// <returns></returns>
    [OneTimeSetUp]
    public async Task Setup()
    {
        var storageDirectory = FileLocationTools.DefaultStorageDirectory();

        TestDirectory =
            storageDirectory.CreateSubdirectory($"TempCloudBackupTest-{DateTime.Now:yyyy-MM-dd-hh-mm-ss-ff}");

        TestDirectory1 = TestDirectory.CreateSubdirectory("TestL");
        TestDirectory2 = TestDirectory.CreateSubdirectory("TestR");
        TestDirectory3 = TestDirectory2.CreateSubdirectory("R1");
        TestDirectory4 = TestDirectory3.CreateSubdirectory("R2L");
        TestDirectory5 = TestDirectory3.CreateSubdirectory("R2R");

        var testDb = Path.Combine(TestDirectory.FullName, "TestDb.db");

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

    /// <summary>
    ///     Make sure all directories are counted.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task T000_BasicIntegrationNoExclusions()
    {
        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJob.SingleAsync();
        var directories = await CreationTools.GetAllLocalDirectories(job);

        Assert.That(directories, Has.Count.EqualTo(6));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory1.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory2.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory3.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory4.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory5.FullName));
    }

    /// <summary>
    ///     Confirm a leaf node exact match is excluded
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task T100_LeafNodeDirectoryExactMatchExcluded()
    {
        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJob.SingleAsync();
        context.ExcludedDirectories.Add(new ExcludedDirectory
        {
            CreatedOn = DateTime.Now,
            JobId = job.Id,
            Directory = TestDirectory5.FullName
        });
        await context.SaveChangesAsync();

        var directories = await CreationTools.GetAllLocalDirectories(job);

        Assert.That(directories, Has.Count.EqualTo(5));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory1.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory2.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory3.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory4.FullName));
    }

    /// <summary>
    ///     Add a pattern exclusion that should exclude a leaf node with a * pattern
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task T110_LeafNodeDirectoryPatternMatchExcluded()
    {
        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJob.SingleAsync();
        context.ExcludedDirectoryNamePatterns.Add(new ExcludedDirectoryNamePattern
        {
            CreatedOn = DateTime.Now,
            JobId = job.Id,
            Pattern = "*2L"
        });
        await context.SaveChangesAsync();

        var directories = await CreationTools.GetAllLocalDirectories(job);

        Assert.That(directories, Has.Count.EqualTo(4));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory1.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory2.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory3.FullName));
    }

    /// <summary>
    ///     Add a pattern match exclusion with ?s that should exclude everything but the initial directory
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task T200_NodesWithChildrenDirectoryPatternMatchExcluded()
    {
        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJob.SingleAsync();
        context.ExcludedDirectoryNamePatterns.RemoveRange(context.ExcludedDirectoryNamePatterns);
        context.ExcludedDirectories.RemoveRange(context.ExcludedDirectories);

        context.ExcludedDirectoryNamePatterns.Add(new ExcludedDirectoryNamePattern
        {
            CreatedOn = DateTime.Now,
            JobId = job.Id,
            Pattern = "Test?"
        });
        await context.SaveChangesAsync();

        var directories = await CreationTools.GetAllLocalDirectories(job);

        Assert.That(directories, Has.Count.EqualTo(1));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory.FullName));
    }

    /// <summary>
    ///     Clear out the previous exclusions and add an exact match exclusion that should
    ///     exclude a node with children
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task T210_NodeWithChildrenDirectoriesExactMatchExcluded()
    {
        var context = await CloudBackupContext.CreateInstance();
        var job = await context.BackupJob.SingleAsync();
        context.ExcludedDirectoryNamePatterns.RemoveRange(context.ExcludedDirectoryNamePatterns);
        context.ExcludedDirectories.RemoveRange(context.ExcludedDirectories);

        context.ExcludedDirectories.Add(new ExcludedDirectory
        {
            CreatedOn = DateTime.Now,
            JobId = job.Id,
            Directory = TestDirectory3.FullName
        });
        await context.SaveChangesAsync();

        var directories = await CreationTools.GetAllLocalDirectories(job);

        Assert.That(directories, Has.Count.EqualTo(3));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory1.FullName));
        Assert.That(directories.Select(x => x.FullName), Has.Exactly(1).EqualTo(TestDirectory2.FullName));
    }
#pragma warning disable CS8618
    public DirectoryInfo TestDirectory { get; set; }
    public DirectoryInfo TestDirectory1 { get; set; }
    public DirectoryInfo TestDirectory2 { get; set; }
    public DirectoryInfo TestDirectory3 { get; set; }
    public DirectoryInfo TestDirectory4 { get; set; }
    public DirectoryInfo TestDirectory5 { get; set; }
#pragma warning restore CS8618
}