using System.Reflection;
using System.Text.RegularExpressions;

namespace PointlessWaymarks.CommonTools;

public static class ProgramInfoTools
{
    public static DateTime? GetBuildDate(Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<BuildDateAttribute>();
        return attribute?.DateTime;
    }

    public static DateTime? GetEntryAssemblyBuildDate()
    {
        try
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null) return null;
            var attribute = entryAssembly.GetCustomAttribute<BuildDateAttribute>();
            return attribute?.DateTime;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    public static (string? dateString, FileInfo? setupFile) LatestInstaller(string installerDirectory,
        string baseInstallerName)
    {
        if (string.IsNullOrEmpty(installerDirectory)) return (null, null);
        if (string.IsNullOrEmpty(baseInstallerName)) return (null, null);

        var containingDirectory = new DirectoryInfo(installerDirectory);

        if (!containingDirectory.Exists) return (null, null);

        var publishFile = containingDirectory.GetFiles($"{baseInstallerName}--*.exe").ToList().MaxBy(x => x.Name);

        if (publishFile == null) return (null, null);

        var dateVersionString = Regex
            .Match(publishFile.Name, @".*--(?<dateVersion>\d\d\d\d-\d\d-\d\d-\d\d-\d\d).exe");

        if (!dateVersionString.Groups.ContainsKey("dateVersion")) return (null, null);

        return (dateVersionString.Groups["dateVersion"].Value, publishFile);
    }

    public static (string humanTitleString, string dateVersion, bool isInstalled) StandardAppInformationString(
        Assembly executingAssembly, string appName)
    {
        var humanTitleString = string.Empty;
        var foundInstallVersion = false;
        var dateVersionString = string.Empty;

        try
        {
            humanTitleString += $"{appName}  ";

            if (executingAssembly != null &&
                !string.IsNullOrEmpty(executingAssembly.Location) &&
                !string.IsNullOrEmpty(Path.GetDirectoryName(executingAssembly.Location)))
            {
                var containingDirectory = new DirectoryInfo(Path.GetDirectoryName(executingAssembly.Location));

                if (containingDirectory.Exists)
                {
                    var publishFile = containingDirectory.GetFiles("PublishVersion--*.txt").ToList().MaxBy(x => x.Name);

                    if (publishFile == null)
                    {
                        humanTitleString += " No Version Found";
                    }
                    else
                    {
                        foundInstallVersion = true;

                        humanTitleString +=
                            $" {Path.GetFileNameWithoutExtension(publishFile.Name).Split("--").LastOrDefault()}";

                        humanTitleString += $" {File.ReadAllText(publishFile.FullName)}";

                        dateVersionString = Regex
                            .Match(humanTitleString, @".* (?<dateVersion>\d\d\d\d-\d\d-\d\d-\d\d-\d\d) .*")
                            .Groups["dateVersion"].Value;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return (humanTitleString, dateVersionString, foundInstallVersion);
    }
}