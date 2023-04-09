// See https://aka.ms/new-console-template for more information

using System.Reflection;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.Task.MemoriesEmail;
using Serilog;

LogTools.StandardStaticLoggerForProgramDirectory("MemoriesEmail");

Log.ForContext("args", Helpers.SafeObjectDump(args)).Information(
    "PointlessWaymarks.Task.MemoriesEmail Starting");

Console.WriteLine($"Memories Email - Build {ProgramInfoTools.GetBuildDate(Assembly.GetExecutingAssembly())}");

if (args.Length == 0)
{
    Console.WriteLine("Welcome to the Pointless Waymarks Project Memories Email.");
    Console.WriteLine();
    Console.WriteLine("This program will scan a Pointless Waymarks Project site and");
    Console.WriteLine("  generate an email from content X years back.");
    Console.WriteLine();
    Console.WriteLine("This program takes either:");
    Console.WriteLine(" -authentication - this will start the setup to securely store");
    Console.WriteLine("   your email credentials.");
    Console.WriteLine(" The name of your settings file.");

    return;
}


if (args.Length > 0 && args[0].Contains("-authentication", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("To save a secure login for sending email give it a login code to identify it.");
    Console.WriteLine(" You will need to enter the login_code into your settings file, for example:");
    Console.WriteLine("  \"loginCode\":\"mainGcLogin\"");
    Console.WriteLine(" The loginCode should only contain letters and number - no spaces or symbols.");
    Console.WriteLine(" Specifying an existing login code will overwrite the current username and password.");
    Console.WriteLine();
    Console.Write("Login Code: ");
    var loginCode = Console.ReadLine();

    if (string.IsNullOrEmpty(loginCode))
    {
        Console.WriteLine("Sorry, a blank login code is not valid... Please try again.");
        return;
    }

    if (!loginCode.All(char.IsLetterOrDigit))
    {
        Console.WriteLine(
            "Sorry, login codes must consist of only letters and numbers - no spaces or symbols... Please try again.");
        return;
    }

    Console.WriteLine();

    Console.WriteLine($"Login Code is {loginCode} - set the username and password for this login.");
    Console.WriteLine();

    Console.Write("Username: ");

    var userName = Console.ReadLine();

    if (string.IsNullOrEmpty(userName))
    {
        Console.WriteLine("Sorry, a blank username is not valid... Please try again.");
        return;
    }

    var password = ConsoleTools.GetPasswordFromConsole("Password: ");

    PasswordVaultTools.SaveCredentials(MemoriesSmtpEmailFromWebSettings.PasswordVaultResourceIdentifier(loginCode),
        userName, password);

    Console.WriteLine();

    Console.WriteLine($"Username and password saved for Login Code {loginCode} - ");
    Console.WriteLine("  the line below should appear in your settings file:");
    Console.WriteLine($"  \"loginCode\":\"{loginCode}\"");

    Console.WriteLine();

    return;
}


try
{
    var runner = new MemoriesSmtpEmailFromWeb();
    await runner.GenerateEmail(args[0]);
}
catch (Exception e)
{
    Log.Error(e, "Error Running Program...");
    Console.WriteLine(e);

    await (await WindowsNotificationBuilders.NewNotifier(MemoriesSmtpEmailFromWebSettings.ProgramShortName))
        .SetAutomationLogoNotificationIconUrl()
        .SetErrorReportAdditionalInformationMarkdown(
            FileAndFolderTools.ReadAllText(Path.Combine(AppContext.BaseDirectory, "README.md"))).Error(e);
}