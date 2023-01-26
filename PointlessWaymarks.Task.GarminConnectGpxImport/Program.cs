﻿// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Microsoft.Toolkit.Uwp.Notifications;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.Task.GarminConnectGpxImport;
using Serilog;

LogTools.StandardStaticLoggerForProgramDirectory("GarminConnectGpxImport");

Log.ForContext("args", args.SafeObjectDump()).Information(
    "PointlessWaymarks.Task.GarminConnectGpxImport Starting");

Console.WriteLine(
    $"Garmin Connect Gpx Import - Build {ProgramInfoTools.GetBuildDate(Assembly.GetExecutingAssembly())}");

if (args.Length == 0)
{
    Console.WriteLine("Welcome to the Pointless Waymarks Project Garmin Connect Gpx Import.");
    Console.WriteLine();
    Console.WriteLine("The intent is for this program to make it easy to setup a Task with");
    Console.WriteLine("  Windows Task Scheduler that can download Connect activities and");
    Console.WriteLine("  import them into a Pointless Waymarks CMS site.");
    Console.WriteLine();
    Console.WriteLine("This program takes either:");
    Console.WriteLine(" -authentication - this will start the setup to securely store");
    Console.WriteLine("   your Garmin Connect Credentials.");
    Console.WriteLine(" The name of your settings file.");

    return;
}

if (args.Length > 0 && args[0].Contains("-authentication", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("To save a secure login for Garmin Connect give it a login code to identify it.");
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

    Console.Write("Username: ");

    var userName = Console.ReadLine();

    if (string.IsNullOrEmpty(userName))
    {
        Console.WriteLine("Sorry, a blank username is not valid... Please try again.");
        return;
    }

    Console.WriteLine();

    var password = ConsoleTools.GetPasswordFromConsole("Password: ");

    PasswordVaultTools.SaveCredentials(GarminConnectGpxImportSettings.PasswordVaultResourceIdentifier(loginCode),
        userName, password);

    Console.WriteLine($"Username and password saved for Login Code {loginCode} - ");
    Console.WriteLine("  the line below should appear in your settings file:");
    Console.WriteLine($"  \"loginCode\":\"{loginCode}\"");

    return;
}

try
{
    var runner = new GpxTrackImport();
    await runner.Import(args[0]);
}
catch (Exception e)
{
    Log.Error(e, "Error Running Program...");
    Console.WriteLine(e);

    new ToastContentBuilder()
        .AddAppLogoOverride(new Uri(
            $"file://{Path.Combine(AppContext.BaseDirectory, "PointlessWaymarksCmsAutomationSquareLogo.png")}"))
        .AddText($"Error: {e.Message}")
        .AddToastActivationInfo(AppContext.BaseDirectory, ToastActivationType.Protocol)
        .AddAttributionText("Pointless Waymarks Project - Garmin Connect Gpx Import")
        .Show();
}