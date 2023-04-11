using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupGui.Models;
using SQLitePCL;

namespace PointlessWaymarks.CloudBackupData;

public class CloudBackupContext : DbContext
{
    public CloudBackupContext(DbContextOptions<CloudBackupContext> options) : base(options)
    {
    }

    public DbSet<BackupBatch> BackupBatches { get; set; }
    public DbSet<BackupSetting> BackupSettings { get; set; }
    public DbSet<CloudDelete> CloudDeletions { get; set; }
    public DbSet<CloudUpload> CloudUploads { get; set; }
    public DbSet<ExcludedDirectory> ExcludedDirectories { get; set; }
    public DbSet<FileSystemFile> FileSystemFiles { get; set; }
    public DbSet<IncludedDirectory> IncludedDirectories { get; set; }

    public static Task<CloudBackupContext> CreateInstance(string fileName)
    {
        // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
        Batteries_V2.Init();
        raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);

        var optionsBuilder = new DbContextOptionsBuilder<CloudBackupContext>();
        return Task.FromResult(new CloudBackupContext(optionsBuilder.UseSqlite($"Data Source={fileName}").Options));
    }

    public static async Task<CloudBackupContext> CreateInstanceWithEnsureCreated(string fileName)
    {
        var context = await CreateInstance(fileName);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=PointlessWaymarksCloudBackup.db");
    }
}