// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using PointlessWaymarks.MemoriesEmail;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().Enrich.WithProcessId().Enrich.WithProcessName().Enrich.WithThreadId()
    .Enrich.WithThreadName().Enrich.WithMachineName().Enrich.WithEnvironmentUserName().WriteTo.Console().WriteTo.File(new RenderedCompactJsonFormatter(),
        Path.Combine(AppContext.BaseDirectory,
            "PointlessWaymarksCms-MemoriesEmail-.json"), rollingInterval: RollingInterval.Day, shared: true).CreateLogger();

var host = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
{
    services.AddTransient<IMemoriesSmtpEmailFromWeb, MemoriesSmtpEmailFromWeb>();
}).UseSerilog().Build();

var settingsFileArgument = new Argument<string>
    ("settingsFile", "You must pass in the name of a settings file to use.");

var rootCommand = new RootCommand("Pointless Waymarks CMS Memories Email Generator") { settingsFileArgument };

rootCommand.SetHandler(async (settingsFile) =>
{
    try
    {
        var emailRunner = ActivatorUtilities.CreateInstance<IMemoriesSmtpEmailFromWeb>(host.Services);
        await emailRunner.GenerateEmail(settingsFile);
    }
    catch (Exception e)
    {
        Log.Error(e, "Error Creating Email");
        Console.WriteLine(e);
        throw;
    }

}, settingsFileArgument);

await rootCommand.InvokeAsync(args);