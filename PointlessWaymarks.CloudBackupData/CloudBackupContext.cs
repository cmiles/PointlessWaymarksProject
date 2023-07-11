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
    public DbSet<BackupJob> BackupJobs { get; set; }
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

        return Task.FromResult(new CloudBackupContext(optionsBuilder.UseLazyLoadingProxies().UseSqlite($"Data Source={fileName}").Options));
    }

    /// <summary>
    /// Use TryCreateInstance to test whether an input file is a valid db.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="setFileNameAsCurrentDb"></param>
    /// <returns></returns>
    public static Task<(bool success, string message, CloudBackupContext? context)> TryCreateInstance(string fileName, bool setFileNameAsCurrentDb = true)
    {
        // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
        Batteries_V2.Init();
        raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        var optionsBuilder = new DbContextOptionsBuilder<CloudBackupContext>();

        CloudBackupContext? db = null;
        
        try
        {
            db = new CloudBackupContext(optionsBuilder.UseLazyLoadingProxies().UseSqlite($"Data Source={fileName}").Options);
        }
        catch (Exception e)
        {
            return Task.FromResult<(bool success, string Empty, CloudBackupContext? context)>((false, e.Message, null));
        }
        
        if (setFileNameAsCurrentDb) CurrentDatabaseFileName = fileName;

        return Task.FromResult<(bool success, string Empty, CloudBackupContext? context)>((true, string.Empty, db));
    }
    
    public static async Task<CloudBackupContext> CreateInstanceWithEnsureCreated(string fileName,
        bool setFileNameAsCurrentDb = true)
    {
        var context = await CreateInstance(fileName, setFileNameAsCurrentDb);
        await context.Database.EnsureCreatedAsync();

        return context;
    }
}