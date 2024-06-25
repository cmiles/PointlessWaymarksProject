using System.Diagnostics;
using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PointlessWaymarks.PowerShellRunnerData.Models;
using Serilog;
using SQLitePCL;

namespace PointlessWaymarks.PowerShellRunnerData;

public class PowerShellRunnerContext(DbContextOptions<PowerShellRunnerContext> options) : DbContext(options)
{
    public static string CurrentDatabaseFileName = string.Empty;
    public DbSet<ScriptJobRun> ScriptJobRuns { get; set; } = null!;
    public DbSet<ScriptJob> ScriptJobs { get; set; } = null!;
    public DbSet<PowerShellRunnerSetting> PowerShellRunnerSettings { get; set; } = null!;

    public static async Task<PowerShellRunnerContext> CreateInstance()
    {
        return await CreateInstance(CurrentDatabaseFileName);
    }

    public static Task<PowerShellRunnerContext> CreateInstance(string fileName, bool setFileNameAsCurrentDb = true)
    {
        // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
        Batteries_V2.Init();
        raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        var optionsBuilder = new DbContextOptionsBuilder<PowerShellRunnerContext>();

        optionsBuilder.LogTo(message => Debug.WriteLine(message));

        if (setFileNameAsCurrentDb) CurrentDatabaseFileName = fileName;

        return Task.FromResult(new PowerShellRunnerContext(optionsBuilder
            .UseSqlite($"Data Source={fileName}").Options));
    }

    public static async Task<PowerShellRunnerContext> CreateInstanceWithEnsureCreated(string fileName,
        bool setFileNameAsCurrentDb = true)
    {
        if (File.Exists(fileName))
        {
            var sc = new ServiceCollection().AddFluentMigratorCore().ConfigureRunner(rb =>
                    rb.AddSQLite()
                        .WithGlobalConnectionString(
                            $"Data Source={fileName}")
                        .ScanIn(typeof(PowerShellRunnerContext).Assembly).For.Migrations())
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
    /// <param name="createFileIfDoesNotExist"></param>
    /// <returns></returns>
    public static async Task<(bool success, string message, PowerShellRunnerContext? context)> TryCreateInstance(
        string fileName,
        bool setFileNameAsCurrentDb = true, bool createFileIfDoesNotExist = false)
    {
        var newFileInfo = new FileInfo(fileName);

        if (!newFileInfo.Exists)
            if (createFileIfDoesNotExist)
            {
                var createContext = await CreateInstance(fileName, setFileNameAsCurrentDb);
                await createContext.Database.EnsureCreatedAsync();
                await createContext.DisposeAsync();
            }
            else
            {
                return (false,
                    "File does not exist?", null);
            }

        try
        {
            await using var sc = new ServiceCollection().AddFluentMigratorCore().ConfigureRunner(rb =>
                    rb.AddSQLite()
                        .WithGlobalConnectionString(
                            $"Data Source={fileName}")
                        .ScanIn(typeof(PowerShellRunnerContext).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddSerilog()).BuildServiceProvider(false);

            using var scope = sc.CreateScope();
            // Instantiate the runner
            var runner = sc.GetRequiredService<IMigrationRunner>();

            // Execute the migrations
            runner.MigrateUp();
        }
        catch (Exception e)
        {
            return (false, e.Message, null);
        }

        // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
        Batteries_V2.Init();
        raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        var optionsBuilder = new DbContextOptionsBuilder<PowerShellRunnerContext>();

        PowerShellRunnerContext? db;

        try
        {
            db = new PowerShellRunnerContext(optionsBuilder.UseSqlite($"Data Source={fileName}")
                .Options);
        }
        catch (Exception e)
        {
            return (false, e.Message, null);
        }

        if (setFileNameAsCurrentDb) CurrentDatabaseFileName = fileName;

        return (true, string.Empty, db);
    }
}