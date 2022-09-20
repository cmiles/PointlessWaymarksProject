// See https://aka.ms/new-console-template for more information

using Serilog;
using Serilog.Formatting.Compact;
using PointlessWaymarks.MemoriesEmail;

Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().Enrich.WithProcessId().Enrich.WithProcessName().Enrich.WithThreadId()
    .Enrich.WithThreadName().Enrich.WithMachineName().Enrich.WithEnvironmentUserName().WriteTo.Console().WriteTo.File(new RenderedCompactJsonFormatter(),
        Path.Combine(AppContext.BaseDirectory,
            "PointlessWaymarksCms-MemoriesEmail-.json"), rollingInterval: RollingInterval.Day, shared: true).CreateLogger();

if(args.Length != 1) Console.WriteLine("Please provide a settings file name...");

try
{
    var runner = new MemoriesSmtpEmailFromWeb();
    await runner.GenerateEmail(args[0]);
}
catch (Exception e)
{
    Log.Error(e, "Error Running Program...");
    Console.WriteLine(e);
}