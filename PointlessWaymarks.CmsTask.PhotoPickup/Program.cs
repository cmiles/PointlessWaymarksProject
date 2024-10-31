// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Microsoft.Extensions.Logging;
using PointlessWaymarks.CmsTask.PhotoPickup;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.VaultfuscationTools;
using PointlessWaymarks.WindowsTools;
using Serilog;
using Serilog.Extensions.Logging;

LogTools.StandardStaticLoggerForProgramDirectory("PhotoPickup");

Log.ForContext("args", args.SafeObjectDump()).Information(
    "PointlessWaymarks.CmsTask.PhotoPickup Starting");

Console.WriteLine($"Photo Pickup - Build {ProgramInfoTools.GetBuildDate(Assembly.GetExecutingAssembly())}");

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
    .CreateLogger<ObfuscatedSettingsConsoleSetup<PhotoPickupSettings>>();

var vaultService = "http://garminconnectgpximport.pointlesswaymarks.private";

var settingFileReadAndSetup = new ObfuscatedSettingsConsoleSetup<PhotoPickupSettings>(msLogger)
{
    SettingsFile = cleanedSettingsFile,
    SettingsFileIdentifier = PhotoPickupSettings.SettingsTypeIdentifier(),
    VaultServiceIdentifier = vaultService,
    Interactive = interactive,
    PromptAsIfNewFile = promptAsIfNewFile,
    SettingsFileProperties =
    [
        new SettingsFileProperty<PhotoPickupSettings>
        {
            PropertyDisplayName = "Pointless Waymarks Site Settings File",
            PropertyEntryHelp =
                "The full path to the settings file for the Pointless Waymarks CMS site. This file should be the JSON file that contains the settings for the site.",
            PropertyIsValid = ObfuscatedSettingsHelpers.PropertyIsValidIfFileExists<PhotoPickupSettings>(x => x.PointlessWaymarksSiteSettingsFileFullName),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) => settings.PointlessWaymarksSiteSettingsFileFullName = userEntry.Trim(),
            GetCurrentStringValue = x => x.PointlessWaymarksSiteSettingsFileFullName
        },
        new SettingsFileProperty<PhotoPickupSettings>
        {
            PropertyDisplayName = "Photo Pickup Directory",
            PropertyEntryHelp =
                "The program will look in this directory for files to Pickup.",
            PropertyIsValid =
                ObfuscatedSettingsHelpers
                    .PropertyIsValidIfDirectoryExistsOrCanBeCreated<PhotoPickupSettings>(x =>
                        x.PhotoPickupDirectory),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) => settings.PhotoPickupDirectory = userEntry.Trim(),
            GetCurrentStringValue = x => x.PhotoPickupDirectory
        },
        new SettingsFileProperty<PhotoPickupSettings>
        {
            PropertyDisplayName = "Photo Archive Directory",
            PropertyEntryHelp =
                "Photos found in the Photo Pickup Directory will be written to the Photo Archive Directory if they are successfully added to the Pointless Waymarks Project site.",
            PropertyIsValid =
                ObfuscatedSettingsHelpers
                    .PropertyIsValidIfDirectoryExistsOrCanBeCreated<PhotoPickupSettings>(x =>
                        x.PhotoPickupArchiveDirectory),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) => settings.PhotoPickupArchiveDirectory = userEntry.Trim(),
            GetCurrentStringValue = x => x.PhotoPickupArchiveDirectory
        },
        new SettingsFileProperty<PhotoPickupSettings>
        {
            PropertyDisplayName = "Rename Photograph File to Title",
            PropertyEntryHelp =
                "If enabled the program will rename the Photograph's file on disk to the Title the CMS determines for the Photograph. This can help keep an easy match between the file and the CMS Content - but you may loose valuable information if the filename is changed.",
            PropertyIsValid = _ => (true, ""),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfBool(),
            SetValue = (settings, userEntry) => settings.RenameFileToTitle = bool.Parse(userEntry),
            GetCurrentStringValue = x => x.RenameFileToTitle.ToString()
        },
        new SettingsFileProperty<PhotoPickupSettings>
        {
            PropertyDisplayName = "Show in Site Main Feed",
            PropertyEntryHelp =
                "If enabled the successfully imported Photographs will be added to the site's Main Feed.",
            PropertyIsValid = _ => (true, ""),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfBool(),
            SetValue = (settings, userEntry) => settings.ShowInMainSiteFeed = bool.Parse(userEntry),
            GetCurrentStringValue = x => x.ShowInMainSiteFeed.ToString()
        },

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
    var runner = new PhotoPickup();
    await runner.PickupPhotos(settingsSetupResult.settings);
}
catch (Exception e)
{
    Log.Error(e, "Error Running Program...");
    Console.WriteLine(e);

    await (await WindowsNotificationBuilders.NewNotifier(PhotoPickupSettings.ProgramShortName()))
        .SetAutomationLogoNotificationIconUrl().SetErrorReportAdditionalInformationMarkdown(
            EmbeddedResourceTools.GetEmbeddedResourceText("README.md")).Error(e);
}
finally
{ 
    Log.CloseAndFlush();
}