// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().Enrich.WithProcessId().Enrich.WithProcessName().Enrich.WithThreadId()
    .Enrich.WithThreadName().Enrich.WithMachineName().Enrich.WithEnvironmentUserName().WriteTo.Console().WriteTo.File(new RenderedCompactJsonFormatter(),
        Path.Combine(AppContext.BaseDirectory,
            "PointlessWaymarksCms-MemoriesEmail-.json"), rollingInterval: RollingInterval.Day, shared: true).CreateLogger();

var host = Host.CreateDefaultBuilder().ConfigureServices((context, services) => {}).UseSerilog().Build();