using System.Reflection;
using Microsoft.Extensions.Logging;
using PointlessWaymarks.CmsTask.PublishSiteToS3;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.VaultfuscationTools;
using PointlessWaymarks.WindowsTools;
using Serilog;
using Serilog.Extensions.Logging;

LogTools.StandardStaticLoggerForProgramDirectory("PublishToS3");

Log.ForContext("args", args.SafeObjectDump()).Information(
    "PointlessWaymarks.CmsTask.PublishToS3 Starting");

Console.WriteLine(
    $"Publish Site To Amazon S3 - Build {ProgramInfoTools.GetBuildDate(Assembly.GetExecutingAssembly())}");

if (args.Length == 0 || (args.Length == 1 && (args[0].Equals("help") || args[0].Equals("-help"))))
{
    ConsoleTools.WriteWrappedTextBlock("""
                                       Welcome to the Pointless Waymarks Project Publish to S3 Task. This program can automatically generate and upload a Pointless Waymarks Project site to an S3 bucket.

                                       """);

    VaultfuscationMessages.VaultfuscationWarning();

    ConsoleTools.WriteWrappedTextBlock("""

                                       On the command line you must specify:
                                        - The name of the settings file. You can specify the name of a new file and the program will prompt you for the settings to use and then save them to the file. By default the program will also prompt the user to enter settings if there are any missing or have invalid values - see the '-notinteractive' flag below to control this behavior.
                                        
                                       You can also specify:
                                        - '-notinteractive': By default the program will prompt the user for input if the settings file is not found or settings are not valid. If you specify -notinteractive the program will exit with an error message if the settings file is not found or settings are not valid. This must be specified after the settings file name.
                                       """);
}

if (args.Length == 0 || (args.Length == 1 && (args[0].Equals("help") || args[0].Equals("-help"))))
{
    ConsoleTools.WriteWrappedTextBlock("""
                                       Welcome to the Pointless Waymarks Project Photo Pickup Task. This program can scan a directory for photos, import them to a Pointless Waymarks Project site and archive the files. This can allow you to easily import photographs from a 'shared' directory like a local Dropbox or Google Drive folder.

                                       """);

    VaultfuscationMessages.VaultfuscationWarning();

    ConsoleTools.WriteWrappedTextBlock("""

                                       On the command line you must specify:
                                        - The name of the settings file. You can specify the name of a new file and the program will prompt you for the settings to use and then save them to the file. By default the program will also prompt the user to enter settings if there are any missing or have invalid values - see the '-notinteractive' flag below to control this behavior.
                                        
                                       You can also specify:
                                        - '-notinteractive': By default the program will prompt the user for input if the settings file is not found or settings are not valid. If you specify -notinteractive the program will exit with an error message if the settings file is not found or settings are not valid. This must be specified after the settings file name.
                                       """);
}

var cleanedSettingsFile = args[0].Trim();

var interactive = !args.Any(x => x.Contains("-notinteractive", StringComparison.OrdinalIgnoreCase));
var promptAsIfNewFile = args.Any(x => x.Contains("-redo", StringComparison.OrdinalIgnoreCase));

var msLogger = new SerilogLoggerFactory(Log.Logger)
    .CreateLogger<ObfuscatedSettingsConsoleSetup<PublishToS3Settings>>();

var vaultService = "http://garminconnectgpximport.pointlesswaymarks.private";

var settingFileReadAndSetup = new ObfuscatedSettingsConsoleSetup<PublishToS3Settings>(msLogger)
{
    SettingsFile = cleanedSettingsFile,
    SettingsFileIdentifier = PublishToS3Settings.SettingsTypeIdentifier(),
    VaultServiceIdentifier = vaultService,
    Interactive = interactive,
    PromptAsIfNewFile = promptAsIfNewFile,
    SettingsFileProperties =
    [
        new SettingsFileProperty<PublishToS3Settings>
        {
            PropertyDisplayName = "Pointless Waymarks Site Settings File",
            PropertyEntryHelp =
                "The full path to the settings file for the Pointless Waymarks CMS site. This file should be the JSON file that contains the settings for the site.",
            PropertyIsValid =
                ObfuscatedSettingsHelpers.PropertyIsValidIfFileExists<PublishToS3Settings>(x =>
                    x.PointlessWaymarksSiteSettingsFileFullName),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) => settings.PointlessWaymarksSiteSettingsFileFullName = userEntry.Trim(),
            GetCurrentStringValue = x => x.PointlessWaymarksSiteSettingsFileFullName
        }
    ]
};

var settingsSetupResult = await settingFileReadAndSetup.Setup();

if (!settingsSetupResult.isValid)
{
    Console.WriteLine("");
    ConsoleTools.WriteLineRedWrappedTextBlock(
        $"Settings Setup FAILED - setup returned isValid: {settingsSetupResult.isValid}");
    ConsoleTools.WriteLineRedWrappedTextBlock($"{settingsSetupResult.settings}", indent: 2);
    return;
}


try
{
    var runner = new PublishToS3();
    await runner.Publish(settingsSetupResult.settings);
}
catch (Exception e)
{
    Log.Error(e, "Error Running Program...");
    Console.WriteLine(e);

    await (await WindowsNotificationBuilders.NewNotifier(PublishToS3Settings.ProgramShortName()))
        .SetAutomationLogoNotificationIconUrl()
        .SetErrorReportAdditionalInformationMarkdown(
            FileAndFolderTools.ReadAllText(Path.Combine(AppContext.BaseDirectory, "README_Task-PublishToS3.md")))
        .Error(e);
}
finally
{
    Log.CloseAndFlush();
}