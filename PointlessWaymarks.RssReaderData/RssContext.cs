using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PointlessWaymarks.RssReaderData.Models;
using Serilog;
using SQLitePCL;

namespace PointlessWaymarks.RssReaderData;

public class RssContext : DbContext
{
    public static string CurrentDatabaseFileName = string.Empty;

    public RssContext(DbContextOptions<RssContext> options) : base(options)
    {
    }

    public DbSet<RssFeed> RssFeeds { get; set; } = null!;

    public DbSet<RssItem> RssItems { get; set; } = null!;

    public static async Task<RssContext> CreateInstance()
    {
        return await CreateInstance(CurrentDatabaseFileName);
    }

    public static Task<RssContext> CreateInstance(string fileName, bool setFileNameAsCurrentDb = true)
    {
        // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
        Batteries_V2.Init();
        raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        var optionsBuilder = new DbContextOptionsBuilder<RssContext>();

        if (setFileNameAsCurrentDb) CurrentDatabaseFileName = fileName;

        return Task.FromResult(new RssContext(optionsBuilder
            .UseSqlite($"Data Source={fileName}").Options));
    }

    public static async Task<RssContext> CreateInstanceWithEnsureCreated(string fileName,
        bool setFileNameAsCurrentDb = true)
    {
        if (File.Exists(fileName))
        {
            var sc = new ServiceCollection().AddFluentMigratorCore().ConfigureRunner(rb =>
                    rb.AddSQLite()
                        .WithGlobalConnectionString(
                            $"Data Source={fileName}")
                        .ScanIn(typeof(RssContext).Assembly).For.Migrations())
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
    public static Task<(bool success, string message, RssContext? context)> TryCreateInstance(string fileName,
        bool setFileNameAsCurrentDb = true)
    {
        var newFileInfo = new FileInfo(fileName);

        if (!newFileInfo.Exists)
            return Task.FromResult<(bool success, string message, RssContext? context)>((false,
                "File does not exist?", null));

        try
        {
            var sc = new ServiceCollection().AddFluentMigratorCore().ConfigureRunner(rb =>
                    rb.AddSQLite()
                        .WithGlobalConnectionString(
                            $"Data Source={fileName}")
                        .ScanIn(typeof(RssContext).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddSerilog()).BuildServiceProvider(false);

            // Instantiate the runner
            var runner = sc.GetRequiredService<IMigrationRunner>();

            // Execute the migrations
            runner.MigrateUp();
        }
        catch (Exception e)
        {
            return Task.FromResult<(bool success, string message, RssContext? context)>(
                (false, e.Message, null));
        }

        // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
        Batteries_V2.Init();
        raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        var optionsBuilder = new DbContextOptionsBuilder<RssContext>();

        RssContext? db;

        try
        {
            db = new RssContext(optionsBuilder.UseSqlite($"Data Source={fileName}")
                .Options);
        }
        catch (Exception e)
        {
            return Task.FromResult<(bool success, string message, RssContext? context)>(
                (false, e.Message, null));
        }

        if (setFileNameAsCurrentDb) CurrentDatabaseFileName = fileName;

        return Task.FromResult<(bool success, string message, RssContext? context)>((true, string.Empty, db));
    }
}