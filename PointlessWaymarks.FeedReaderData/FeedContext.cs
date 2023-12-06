using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PointlessWaymarks.FeedReaderData.Models;
using Serilog;
using SQLitePCL;

namespace PointlessWaymarks.FeedReaderData;

public class FeedContext : DbContext
{
    public FeedContext(DbContextOptions<FeedContext> options) : base(options)
    {
    }

    public DbSet<ReaderFeedItem> FeedItems { get; set; } = null!;

    public DbSet<ReaderFeed> Feeds { get; set; } = null!;

    public DbSet<HistoricReaderFeedItem> HistoricFeedItems { get; set; } = null!;

    public DbSet<HistoricReaderFeed> HistoricFeeds { get; set; } = null!;

    public DbSet<HistoricSavedFeedItem> HistoricSavedFeedItems { get; set; } = null!;

    public DbSet<SavedFeedItem> SavedFeedItems { get; set; } = null!;


    public static Task<FeedContext> CreateInstance(string fileName)
    {
        // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
        Batteries_V2.Init();
        raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        var optionsBuilder = new DbContextOptionsBuilder<FeedContext>();

        return Task.FromResult(new FeedContext(optionsBuilder
            .UseSqlite($"Data Source={fileName}").Options));
    }

    public static async Task<FeedContext> CreateInstanceWithEnsureCreated(string fileName)
    {
        if (File.Exists(fileName))
        {
            await using var sc = new ServiceCollection().AddFluentMigratorCore().ConfigureRunner(rb =>
                    rb.AddSQLite()
                        .WithGlobalConnectionString(
                            $"Data Source={fileName}")
                        .ScanIn(typeof(FeedContext).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddSerilog()).BuildServiceProvider(false);

            using var scope = sc.CreateScope();
            // Instantiate the runner
            var runner = sc.GetRequiredService<IMigrationRunner>();

            // Execute the migrations
            runner.MigrateUp();
        }

        var context = await CreateInstance(fileName);
        await context.Database.EnsureCreatedAsync();

        return context;
    }

    /// <summary>
    ///     Use TryCreateInstance to test whether an input file is a valid db.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static Task<(bool success, string message, FeedContext? context)> TryCreateInstance(string fileName)
    {
        var newFileInfo = new FileInfo(fileName);

        if (!newFileInfo.Exists)
            return Task.FromResult<(bool success, string message, FeedContext? context)>((false,
                "File does not exist?", null));

        try
        {
            var sc = new ServiceCollection().AddFluentMigratorCore().ConfigureRunner(rb =>
                    rb.AddSQLite()
                        .WithGlobalConnectionString(
                            $"Data Source={fileName}")
                        .ScanIn(typeof(FeedContext).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddSerilog()).BuildServiceProvider(false);

            // Instantiate the runner
            var runner = sc.GetRequiredService<IMigrationRunner>();

            // Execute the migrations
            runner.MigrateUp();
        }
        catch (Exception e)
        {
            return Task.FromResult<(bool success, string message, FeedContext? context)>(
                (false, e.Message, null));
        }

        // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
        Batteries_V2.Init();
        raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        var optionsBuilder = new DbContextOptionsBuilder<FeedContext>();

        FeedContext? db;

        try
        {
            db = new FeedContext(optionsBuilder.UseSqlite($"Data Source={fileName}")
                .Options);
        }
        catch (Exception e)
        {
            return Task.FromResult<(bool success, string message, FeedContext? context)>(
                (false, e.Message, null));
        }

        return Task.FromResult<(bool success, string message, FeedContext? context)>((true, string.Empty, db));
    }
}