// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Microsoft.Extensions.Logging;
using PointlessWaymarks.CmsTask.MemoriesEmail;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.VaultfuscationTools;
using PointlessWaymarks.WindowsTools;
using Serilog;
using Serilog.Extensions.Logging;

LogTools.StandardStaticLoggerForProgramDirectory("MemoriesEmail");

Log.ForContext("args", Helpers.SafeObjectDump(args)).Information(
    "PointlessWaymarks.Task.MemoriesEmail Starting");

Console.WriteLine($"Memories Email - Build {ProgramInfoTools.GetBuildDate(Assembly.GetExecutingAssembly())}");

if (args.Length == 0 || (args.Length == 1 && (args[0].Equals("help") || args[0].Equals("-help"))))
{
    ConsoleTools.WriteWrappedTextBlock("""
                                       Welcome to the Pointless Waymarks Project Memories Email. This program can scan a Pointless Waymarks Project site and generate an email from content X years back.

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
    .CreateLogger<ObfuscatedSettingsConsoleSetup<MemoriesSmtpEmailFromWebSettings>>();

var vaultService = "http://memoriesemail.pointlesswaymarks.private";

var settingFileReadAndSetup = new ObfuscatedSettingsConsoleSetup<MemoriesSmtpEmailFromWebSettings>(msLogger)
{
    SettingsFile = cleanedSettingsFile,
    SettingsFileIdentifier = MemoriesSmtpEmailFromWebSettings.SettingsTypeIdentifier(),
    VaultServiceIdentifier = vaultService,
    Interactive = interactive,
    PromptAsIfNewFile = promptAsIfNewFile,
    SettingsFileProperties =
    [
        new SettingsFileProperty<MemoriesSmtpEmailFromWebSettings>
        {
            PropertyDisplayName = "From Email Address",
            PropertyEntryHelp =
                "The email address that the email will be sent from. This email address must be valid and the email account must be able to send email through SMTP.",
            PropertyIsValid =
                ObfuscatedSettingsHelpers.PropertyIsValidIfNotNullOrWhiteSpace<MemoriesSmtpEmailFromWebSettings>(x =>
                    x.FromEmailAddress),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) => settings.FromEmailAddress = userEntry.Trim(),
            GetCurrentStringValue = x => x.FromEmailAddress
        },
        new SettingsFileProperty<MemoriesSmtpEmailFromWebSettings>
        {
            PropertyDisplayName = "From Email Password",
            PropertyEntryHelp =
                "2FA is not directly supported and blank is not a valid value. Be sure to read the program warnings about security before entering this value. For some accounts, especially if 2FA is enabled, you may need to use an 'application password' or similar token rather than your regular password",
            HideEnteredValue = true,
            PropertyIsValid =
                ObfuscatedSettingsHelpers.PropertyIsValidIfNotNullOrWhiteSpace<MemoriesSmtpEmailFromWebSettings>(x =>
                    x.FromEmailPassword),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) => settings.FromEmailPassword = userEntry.Trim(),
            GetCurrentStringValue = x => x.FromEmailPassword
        },
        new SettingsFileProperty<MemoriesSmtpEmailFromWebSettings>
        {
            PropertyDisplayName = "From Email Display Name",
            PropertyEntryHelp = "The display name for the email address that the email will be sent from. This can contain spaces and while it may show in an email client is essentially just for display.",
            PropertyIsValid =
                ObfuscatedSettingsHelpers.PropertyIsValidIfNotNullOrWhiteSpace<MemoriesSmtpEmailFromWebSettings>(x =>
                    x.FromEmailDisplayName),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) => settings.FromEmailDisplayName = userEntry.Trim(),
            GetCurrentStringValue = x => x.FromEmailDisplayName
        },
        new SettingsFileProperty<MemoriesSmtpEmailFromWebSettings>
        {
            PropertyDisplayName = "SMTP Host",
            PropertyEntryHelp = "For example smtp.gmail.com",
            PropertyIsValid =
                ObfuscatedSettingsHelpers.PropertyIsValidIfNotNullOrWhiteSpace<MemoriesSmtpEmailFromWebSettings>(x =>
                    x.SmtpHost),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) => settings.SmtpHost = userEntry.Trim(),
            GetCurrentStringValue = x => x.SmtpHost
        },
        new SettingsFileProperty<MemoriesSmtpEmailFromWebSettings>
        {
            PropertyDisplayName = "SMTP Port",
            PropertyIsValid =
                ObfuscatedSettingsHelpers.PropertyIsValidIfPositiveInt<MemoriesSmtpEmailFromWebSettings>(x =>
                    x.SmtpPort),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfInt(),
            SetValue = (settings, userEntry) => settings.SmtpPort = int.Parse(userEntry),
            GetCurrentStringValue = x => x.SmtpPort.ToString()
        },
        new SettingsFileProperty<MemoriesSmtpEmailFromWebSettings>
        {
            PropertyDisplayName = "SMTP Enable SSL",
            PropertyIsValid = _ => (true, ""),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfBool(),
            SetValue = (settings, userEntry) => settings.SmtpEnableSsl = bool.Parse(userEntry),
            GetCurrentStringValue = x => x.SmtpEnableSsl.ToString()
        },
        new SettingsFileProperty<MemoriesSmtpEmailFromWebSettings>
        {
            PropertyDisplayName = "Site Url",
            PropertyEntryHelp = "The URL of the site to generate the email from. Include the protocol - for example https://yoursite.com",
            PropertyIsValid =
                ObfuscatedSettingsHelpers.PropertyIsValidIfNotNullOrWhiteSpace<MemoriesSmtpEmailFromWebSettings>(x =>
                    x.SiteUrl),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) =>
            {
                if(userEntry.EndsWith("/")) userEntry = userEntry.Substring(0, userEntry.Length - 1);
                settings.SiteUrl = userEntry.Trim();
            },
            GetCurrentStringValue = x => x.SiteUrl
        },
        new SettingsFileProperty<MemoriesSmtpEmailFromWebSettings>
        {
            PropertyDisplayName = "Basic Auth User Name",
            PropertyEntryHelp =
                "If the site for the Memories Email requires basic authentication - enter the user name here, otherwise leave blank.",
            PropertyIsValid = _ => (true, ""),
            UserEntryIsValid = _ => (true, ""),
            SetValue = (settings, userEntry) => settings.BasicAuthUserName = userEntry.Trim(),
            GetCurrentStringValue = x => x.BasicAuthUserName
        },
        new SettingsFileProperty<MemoriesSmtpEmailFromWebSettings>
        {
            PropertyDisplayName = "Basic Auth Password",
            PropertyEntryHelp =
                "If the site for the Memories Email requires basic authentication - enter the password here, otherwise leave blank.",
            HideEnteredValue = true,
            PropertyIsValid = _ => (true, ""),
            UserEntryIsValid = _ => (true, ""),
            SetValue = (settings, userEntry) => settings.BasicAuthPassword = userEntry.Trim(),
            GetCurrentStringValue = x => x.BasicAuthPassword
        },
        new SettingsFileProperty<MemoriesSmtpEmailFromWebSettings>
        {
            PropertyDisplayName = "To Address List",
            PropertyEntryHelp = "A semicolon separated list of email addresses to send the email to.",
            PropertyIsValid =
                ObfuscatedSettingsHelpers.PropertyIsValidIfNotNullOrWhiteSpace<MemoriesSmtpEmailFromWebSettings>(x =>
                    x.ToAddressList),
            UserEntryIsValid = ObfuscatedSettingsHelpers.UserEntryIsValidIfNotNullOrWhiteSpace(),
            SetValue = (settings, userEntry) => settings.ToAddressList = userEntry.Trim(),
            GetCurrentStringValue = x => x.ToAddressList
        },
        new SettingsFileProperty<MemoriesSmtpEmailFromWebSettings>
        {
            PropertyDisplayName = "Years Back",
            PropertyEntryHelp = "A list of years back to generate emails for.",
            PropertyIsValid = x =>
            {
                if (!x.YearsBack.Any())
                    return (false, "Years back list is empty - at least one year must be specified.");
                return (true, "");
            },
            UserEntryIsValid = x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    return (false, "Years back list is empty - at least one year must be specified.");
                if (x.Split(',').Any(y => !int.TryParse(y, out _)))
                    return (false, "Years back list contains non-integer values.");
                return (true, "");
            },
            SetValue = (settings, userEntry) => settings.YearsBack = userEntry.Split(',').Select(int.Parse).ToList(),
            GetCurrentStringValue = x => string.Join(", ", x.YearsBack)
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
    var runner = new MemoriesSmtpEmailFromWeb();
    await runner.GenerateEmail(settingsSetupResult.settings);
}
catch (Exception e)
{
    Log.Error(e, "Error Running Program...");
    Console.WriteLine(e);

    await (await WindowsNotificationBuilders.NewNotifier(MemoriesSmtpEmailFromWebSettings.ProgramShortName()))
        .SetAutomationLogoNotificationIconUrl()
        .SetErrorReportAdditionalInformationMarkdown(
            FileAndFolderTools.ReadAllText(Path.Combine(AppContext.BaseDirectory, "README_Task-MemoriesEmail.md")))
        .Error(e);
}
finally
{
    Log.CloseAndFlush();
}