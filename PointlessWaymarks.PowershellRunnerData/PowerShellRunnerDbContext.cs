using System.Diagnostics;
using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PointlessWaymarks.PowerShellRunnerData.Models;
using Serilog;
using SQLitePCL;

namespace PointlessWaymarks.PowerShellRunnerData;

public class PowerShellRunnerDbContext(DbContextOptions<PowerShellRunnerDbContext> options) : DbContext(options)
{
    public DbSet<PowerShellRunnerSetting> PowerShellRunnerSettings { get; set; } = null!;
    public DbSet<ScriptJobRun> ScriptJobRuns { get; set; } = null!;
    public DbSet<ScriptJob> ScriptJobs { get; set; } = null!;

    public static Task<PowerShellRunnerDbContext> CreateInstance(string fileName)
    {
        // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
        Batteries_V2.Init();
        raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        var optionsBuilder = new DbContextOptionsBuilder<PowerShellRunnerDbContext>();

        optionsBuilder.LogTo(message => Debug.WriteLine(message));

        return Task.FromResult(new PowerShellRunnerDbContext(optionsBuilder
            .UseSqlite($"Data Source={fileName}").Options));
    }

    public static async Task<PowerShellRunnerDbContext> CreateInstanceWithEnsureCreated(string fileName,
        bool setFileNameAsCurrentDb = true)
    {
        if (File.Exists(fileName))
        {
            var sc = new ServiceCollection().AddFluentMigratorCore().ConfigureRunner(rb =>
                    rb.AddSQLite()
                        .WithGlobalConnectionString(
                            $"Data Source={fileName}")
                        .ScanIn(typeof(PowerShellRunnerDbContext).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddSerilog()).BuildServiceProvider(false);

            // Instantiate the runner
            var runner = sc.GetRequiredService<IMigrationRunner>();

            // Execute the migrations
            runner.MigrateUp();
        }

        var context = await CreateInstance(fileName);
        await context.Database.EnsureCreatedAsync();
        await context.VerifyOrAddDbId();

        return context;
    }

    /// <summary>
    ///     Use TryCreateInstance to test whether an input file is a valid db.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="createFileIfDoesNotExist"></param>
    /// <returns></returns>
    public static async Task<(bool success, string message, PowerShellRunnerDbContext? context)> TryCreateInstance(
        string fileName, bool createFileIfDoesNotExist = false)
    {
        var newFileInfo = new FileInfo(fileName);

        if (!newFileInfo.Exists)
            if (createFileIfDoesNotExist)
            {
                var createContext = await CreateInstance(fileName);
                await createContext.Database.EnsureCreatedAsync();
                await createContext.VerifyOrAddDbId();
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
                        .ScanIn(typeof(PowerShellRunnerDbContext).Assembly).For.Migrations())
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
        var optionsBuilder = new DbContextOptionsBuilder<PowerShellRunnerDbContext>();

        PowerShellRunnerDbContext? db;

        try
        {
            db = new PowerShellRunnerDbContext(optionsBuilder.UseSqlite($"Data Source={fileName}")
                .Options);
        }
        catch (Exception e)
        {
            return (false, e.Message, null);
        }

        return (true, string.Empty, db);
    }
}