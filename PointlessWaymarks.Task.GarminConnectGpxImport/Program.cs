// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Microsoft.Extensions.Logging;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.Task.GarminConnectGpxImport;
using PointlessWaymarks.VaultfuscationTools;
using PointlessWaymarks.WindowsTools;
using Serilog;
using Serilog.Extensions.Logging;

LogTools.StandardStaticLoggerForProgramDirectory("GarminConnectGpxImport");

Log.ForContext("args", args.SafeObjectDump()).Information(
    "PointlessWaymarks.Task.GarminConnectGpxImport Starting");

Console.WriteLine(
    $"Garmin Connect Gpx Import - Build {ProgramInfoTools.GetBuildDate(Assembly.GetExecutingAssembly())}");

if (args.Length == 0 || (args.Length == 1 && args[0].Contains("help", StringComparison.OrdinalIgnoreCase)))
{
    ConsoleTools.WriteWrappedTextBlock("""
                                       Welcome to the Pointless Waymarks CMS Garmin Connect Gpx Import Task.

                                       The intent is for this program to make it easy to setup a Task with a scheduling program (like the Windows Task Scheduler) that can download Connect activities and import them into a Pointless Waymarks CMS site.

                                       """);

    VaultfuscationMessages.VaultfuscationWarning();

    ConsoleTools.WriteWrappedTextBlock("""
                                       This program will NOT prevent activities from being imported multiple times - the intended use is for the program to be run at regular intervals and to import the previous _ days without overlap. For example you might run the program daily and always import just the previous day's activities. This is not ideal for some uses but has the advantage that it is clean and simple to understand - no 'sync' that might overwrite custom changes you made to the activity in the CMS, no need to keep a tracking id or worry about matching other identifiers into Garmin Connect.

                                       On the command line you must specify:
                                        - The name of the settings file to use for the import. You can specify the name of a new file and the program will prompt you for the settings to use and then save them to the file. By default the program will also prompt the user to enter any settings that are missing from the settings file or have invalid values - see the '-notinteractive' flag below to control this behavior.
                                        
                                       You can also specify:
                                        - '-notinteractive': By default the program will prompt the user for input if the settings file is not found or settings are not valid. If you specify -notinteractive the program will exit with an error message if the settings file is not found or settings are not valid. This must be specified before -daterange and after the settings file name.

                                       """
    );
    return;
}

var cleanedSettingsFile = args[0].Trim();

var interactive = !args.Any(x => x.Contains("-notinteractive", StringComparison.OrdinalIgnoreCase));

var msLogger = new SerilogLoggerFactory(Log.Logger)
    .CreateLogger<ObfuscatedSettingsConsoleSetup<GarminConnectGpxImportSettings>>();

var vaultService = "http://garminconnectgpximport.pointlesswaymarks.private";

var settingFileReadAndSetup = new ObfuscatedSettingsConsoleSetup<GarminConnectGpxImportSettings>(msLogger)
{
    SettingsFile = cleanedSettingsFile,
    SettingsFileIdentifier = GarminConnectGpxImportSettings.SettingsTypeIdentifier(),
    VaultServiceIdentifier = vaultService,
    Interactive = interactive,
    SettingsFileProperties =
    [
        new SettingsFileProperty<GarminConnectGpxImportSettings>
        {
            PropertyDisplayName = "Garmin Connect Username",
            PropertyIsValid =
                ObfuscatedSettingsHelpers.PropertyIsValidIfNotNullOrWhiteSpace<GarminConnectGpxImportSettings>(x =>
                    x.ConnectUserName),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) => settings.ConnectUserName = userEntry.Trim(),
            GetCurrentStringValue = x => x.ConnectUserName
        },
        new SettingsFileProperty<GarminConnectGpxImportSettings>
        {
            PropertyDisplayName = "Garmin Connect Password",
            PropertyEntryHelp =
                "2FA is not currently supported and blank is not a valid value. Be sure to read the program warnings about security before entering this value.",
            HideEnteredValue = true,
            PropertyIsValid =
                ObfuscatedSettingsHelpers.PropertyIsValidIfNotNullOrWhiteSpace<GarminConnectGpxImportSettings>(x =>
                    x.ConnectPassword),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) => settings.ConnectPassword = userEntry.Trim(),
            GetCurrentStringValue = x => x.ConnectPassword
        },
        new SettingsFileProperty<GarminConnectGpxImportSettings>
        {
            PropertyDisplayName = "GPX Archive Directory",
            PropertyEntryHelp =
                "The directory that the GPX and JSON Files downloaded from Garmin Connect will be written to.",
            PropertyIsValid =
                ObfuscatedSettingsHelpers
                    .PropertyIsValidIfDirectoryExistsOrCanBeCreated<GarminConnectGpxImportSettings>(x =>
                        x.GpxArchiveDirectoryFullName),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) => settings.GpxArchiveDirectoryFullName = userEntry.Trim(),
            GetCurrentStringValue = x => x.GpxArchiveDirectoryFullName
        },
        new SettingsFileProperty<GarminConnectGpxImportSettings>
        {
            PropertyDisplayName = "Overwrite Existing Files in the GPX Archive Directory",
            PropertyEntryHelp =
                "If enabled and any files that share the same name as a downloaded activity will be overwritten. With the Date, Hour and Activity Name in the file name it is likely that the only impact of this setting will be that you will overwrite older files with the latest information from Garmin Connect - the safest setting is 'false' because nothing already on disk can be overwritten by a new activity or by new information.",
            PropertyIsValid = _ => (true, ""),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfBool(),
            SetValue = (settings, userEntry) => settings.OverwriteExistingArchiveDirectoryFiles = bool.Parse(userEntry),
            GetCurrentStringValue = x => x.OverwriteExistingArchiveDirectoryFiles.ToString()
        },
        new SettingsFileProperty<GarminConnectGpxImportSettings>
        {
            PropertyDisplayName = "Days Back",
            PropertyEntryHelp =
                "The number of Days Back the program will look for Activities. This setting should be coordinated with how often you schedule this program to run - for example you might schedule the program to run every day and retrieve 1 day back, the result would be that everyday you get the activities from the previous day.",
            PropertyIsValid =
                ObfuscatedSettingsHelpers.PropertyIsValidIfPositiveInt<GarminConnectGpxImportSettings>(x =>
                    x.DownloadDaysBack),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfInt(),
            SetValue = (settings, userEntry) => settings.DownloadDaysBack = int.Parse(userEntry),
            GetCurrentStringValue = x => x.DownloadDaysBack.ToString()
        },
        new SettingsFileProperty<GarminConnectGpxImportSettings>
        {
            PropertyDisplayName = "Import Activities to Site",
            PropertyEntryHelp =
                "If enabled the program will attempt to import the activities to a Pointless Waymarks CMS site. If disabled the program will only download the activities and write them to the GPX Archive Directory.",
            PropertyIsValid = _ => (true, ""),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfBool(),
            SetValue = (settings, userEntry) => settings.ImportActivitiesToSite = bool.Parse(userEntry),
            GetCurrentStringValue = x => x.ImportActivitiesToSite.ToString()
        },
        new SettingsFileProperty<GarminConnectGpxImportSettings>
        {
            PropertyDisplayName = "Pointless Waymarks Site Settings File",
            PropertyEntryHelp =
                "The full path to the settings file for the Pointless Waymarks CMS site. This file should be a JSON file that contains the settings for the site.",
            ShowForUserEntry = x => x.ImportActivitiesToSite,
            PropertyIsValid = settings =>
            {
                if (settings.ImportActivitiesToSite)
                    return ObfuscatedSettingsHelpers.PropertyIsValidIfFileExists<GarminConnectGpxImportSettings>(x =>
                        x.PointlessWaymarksSiteSettingsFileFullName)(settings);
                else
                    return (true, "");
            },
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) => settings.PointlessWaymarksSiteSettingsFileFullName = userEntry.Trim(),
            GetCurrentStringValue = x => x.PointlessWaymarksSiteSettingsFileFullName
        },
        new SettingsFileProperty<GarminConnectGpxImportSettings>
        {
            PropertyDisplayName = "Show in Main Site Feed",
            PropertyEntryHelp =
                "If enabled the program will attempt to set the imported activities to show in the main site feed. This setting is only relevant if 'Import Activities to Site' is enabled.",
            ShowForUserEntry = x => x.ImportActivitiesToSite,
            PropertyIsValid = _ => (true, ""),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfBool(),
            SetValue = (settings, userEntry) => settings.ShowInMainSiteFeed = bool.Parse(userEntry),
            GetCurrentStringValue = x => x.ShowInMainSiteFeed.ToString()
        },
        new SettingsFileProperty<GarminConnectGpxImportSettings>
        {
            PropertyDisplayName = "Generate Site and Upload After Import",
            PropertyEntryHelp =
                "If enabled the program will attempt to generate the site and upload it after the import is complete. This setting is only relevant if 'Import Activities to Site' is enabled.",
            ShowForUserEntry = x => x.ImportActivitiesToSite,
            PropertyIsValid = _ => (true, ""),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfBool(),
            SetValue = (settings, userEntry) => settings.GenerateSiteAndUploadAfterImport = bool.Parse(userEntry),
            GetCurrentStringValue = x => x.GenerateSiteAndUploadAfterImport.ToString()
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
    var runner = new GpxTrackImport();
    await runner.Import(settingsSetupResult.settings);
}
catch (Exception e)
{
    Log.Error(e, "Error Running Program...");
    Console.WriteLine(e);

    await (await WindowsNotificationBuilders.NewNotifier(GarminConnectGpxImportSettings.ProgramShortName()))
        .SetAutomationLogoNotificationIconUrl()
        .SetErrorReportAdditionalInformationMarkdown(
            FileAndFolderTools.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                "README_Task-GarminConnectGpxImport.md"))).Error(e);
}
finally
{
    Log.CloseAndFlush();
}