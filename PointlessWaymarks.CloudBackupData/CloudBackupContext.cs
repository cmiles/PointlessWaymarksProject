using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PointlessWaymarks.CloudBackupData.Models;
using Serilog;
using SQLitePCL;
using System.Diagnostics;

namespace PointlessWaymarks.CloudBackupData;

public class CloudBackupContext : DbContext
{
    public static string CurrentDatabaseFileName = string.Empty;

    public CloudBackupContext(DbContextOptions<CloudBackupContext> options) : base(options)
    {
    }

    public DbSet<BackupJob> BackupJobs { get; set; } = null!;
    public DbSet<CloudDelete> CloudDeletions { get; set; } = null!;
    public DbSet<CloudTransferBatch> CloudTransferBatches { get; set; } = null!;
    public DbSet<CloudUpload> CloudUploads { get; set; } = null!;
    public DbSet<CloudCacheFile> CloudCacheFiles { get; set; } = null!;
    public DbSet<ExcludedDirectory> ExcludedDirectories { get; set; } = null!;
    public DbSet<ExcludedDirectoryNamePattern> ExcludedDirectoryNamePatterns { get; set; } = null!;
    public DbSet<ExcludedFileNamePattern> ExcludedFileNamePatterns { get; set; } = null!;
    public DbSet<FileSystemFile> FileSystemFiles { get; set; } = null!;
    public DbSet<CloudFile> CloudFiles { get; set; } = null!;

    public static async Task<CloudBackupContext> CreateInstance()
    {
        return await CreateInstance(CurrentDatabaseFileName);
    }

    public static Task<CloudBackupContext> CreateInstance(string fileName, bool setFileNameAsCurrentDb = true)
    {
        // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
        Batteries_V2.Init();
        raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        var optionsBuilder = new DbContextOptionsBuilder<CloudBackupContext>();

        optionsBuilder.LogTo(message => Debug.WriteLine(message));

        if (setFileNameAsCurrentDb) CurrentDatabaseFileName = fileName;

        return Task.FromResult(new CloudBackupContext(optionsBuilder.UseLazyLoadingProxies()
            .UseSqlite($"Data Source={fileName}").Options));
    }

    public static async Task<CloudBackupContext> CreateInstanceWithEnsureCreated(string fileName,
        bool setFileNameAsCurrentDb = true)
    {
        if (File.Exists(fileName))
        {
            var sc = new ServiceCollection().AddFluentMigratorCore().ConfigureRunner(rb =>
                    rb.AddSQLite()
                        .WithGlobalConnectionString(
                            $"Data Source={fileName}")
                        .ScanIn(typeof(CloudBackupContext).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddSerilog()).BuildServiceProvider(false);

            // Instantiate the runner
            var runner = sc.GetRequiredService<IMigrationRunner>();

            // Execute the migrations
            runner.MigrateUp();
        }

        var context = await CreateInstance(fileName, setFileNameAsCurrentDb);
        await context.Database.EnsureCreatedAsync();

        return context;
    }

    /// <summary>
    ///     Use TryCreateInstance to test whether an input file is a valid db.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="setFileNameAsCurrentDb"></param>
    /// <returns></returns>
    public static Task<(bool success, string message, CloudBackupContext? context)> TryCreateInstance(string fileName,
        bool setFileNameAsCurrentDb = true)
    {
        var newFileInfo = new FileInfo(fileName);

        if (!newFileInfo.Exists)
            return Task.FromResult<(bool success, string message, CloudBackupContext? context)>((false,
                "File does not exist?", null));

        try
        {
            var sc = new ServiceCollection().AddFluentMigratorCore().ConfigureRunner(rb =>
                    rb.AddSQLite()
                        .WithGlobalConnectionString(
                            $"Data Source={fileName}")
                        .ScanIn(typeof(CloudBackupContext).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddSerilog()).BuildServiceProvider(false);

            // Instantiate the runner
            var runner = sc.GetRequiredService<IMigrationRunner>();

            // Execute the migrations
            runner.MigrateUp();
        }
        catch (Exception e)
        {
            return Task.FromResult<(bool success, string message, CloudBackupContext? context)>(
                (false, e.Message, null));
        }

        // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
        Batteries_V2.Init();
        raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        var optionsBuilder = new DbContextOptionsBuilder<CloudBackupContext>();

        CloudBackupContext? db;

        try
        {
            db = new CloudBackupContext(optionsBuilder.UseLazyLoadingProxies().UseSqlite($"Data Source={fileName}")
                .Options);
        }
        catch (Exception e)
        {
            return Task.FromResult<(bool success, string message, CloudBackupContext? context)>(
                (false, e.Message, null));
        }

        if (setFileNameAsCurrentDb) CurrentDatabaseFileName = fileName;

        return Task.FromResult<(bool success, string message, CloudBackupContext? context)>((true, string.Empty, db));
    }
}