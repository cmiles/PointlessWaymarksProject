using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData.Models;
using SQLitePCL;

namespace PointlessWaymarks.CloudBackupData;

public class CloudBackupContext : DbContext
{
    public static string CurrentDatabaseFileName = string.Empty;

    public CloudBackupContext(DbContextOptions<CloudBackupContext> options) : base(options)
    {
    }

    public DbSet<CloudTransferBatch> CloudTransferBatches { get; set; }
    public DbSet<BackupJob> BackupJob { get; set; }
    public DbSet<BackupSetting> BackupSettings { get; set; }
    public DbSet<CloudDelete> CloudDeletions { get; set; }
    public DbSet<CloudUpload> CloudUploads { get; set; }
    public DbSet<ExcludedDirectory> ExcludedDirectories { get; set; }
    public DbSet<ExcludedDirectoryNamePattern> ExcludedDirectoryNamePatterns { get; set; }
    public DbSet<ExcludedFileNamePattern> ExcludedFileNamePatterns { get; set; }
    public DbSet<FileSystemFile> FileSystemFiles { get; set; }

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

        if (setFileNameAsCurrentDb) CurrentDatabaseFileName = fileName;

        return Task.FromResult(new CloudBackupContext(optionsBuilder.UseSqlite($"Data Source={fileName}").Options));
    }

    public static async Task<CloudBackupContext> CreateInstanceWithEnsureCreated(string fileName,
        bool setFileNameAsCurrentDb = true)
    {
        var context = await CreateInstance(fileName, setFileNameAsCurrentDb);
        await context.Database.EnsureCreatedAsync();

        return context;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BackupJob>().HasMany(x => x.Batches).WithOne(x => x.Job);
        modelBuilder.Entity<BackupJob>().HasMany(x => x.ExcludedDirectories).WithOne(x => x.Job);
        modelBuilder.Entity<BackupJob>().HasMany(x => x.ExcludedDirectoryNamePatterns).WithOne(x => x.Job);
        modelBuilder.Entity<BackupJob>().HasMany(x => x.ExcludedFileNamePatterns).WithOne(x => x.Job);
        modelBuilder.Entity<BackupJob>().HasMany(x => x.FileSystemFiles).WithOne(x => x.Job);
        modelBuilder.Entity<CloudTransferBatch>().HasMany(x => x.CloudUploads).WithOne(x => x.CloudTransferBatch);
        modelBuilder.Entity<CloudTransferBatch>().HasMany(x => x.CloudDeletions).WithOne(x => x.CloudTransferBatch);
        modelBuilder.Entity<CloudTransferBatch>().HasMany(x => x.FileSystemFiles).WithOne(x => x.CloudTransferBatch);
    }
}