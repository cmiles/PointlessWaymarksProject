using Serilog;

namespace PointlessWaymarks.CommonTools;

public static class UniqueFileTools
{
    public static DirectoryInfo UniqueDirectory(string fullName)
    {
        var directoryInfo = new DirectoryInfo(fullName);

        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
            return directoryInfo;
        }

        var numberLimit = 999;
        var directoryPostfix = 0;
        while (directoryInfo.Exists && directoryPostfix <= numberLimit)
        {
            numberLimit++;
            directoryPostfix++;

            directoryInfo = new DirectoryInfo($"{fullName}-{directoryPostfix:000}");
        }

        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
            return directoryInfo;
        }

        var randomPostfixLimit = 50;
        var randomPostfixCounter = 0;
        while (directoryInfo.Exists && randomPostfixCounter <= randomPostfixLimit)
        {
            randomPostfixLimit++;
            randomPostfixCounter++;

            var postFix = SlugTools.RandomLowerCaseString(6);

            directoryInfo = new DirectoryInfo($"{fullName}-{postFix}");
        }

        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
            return directoryInfo;
        }

        throw new Exception("Can not create a Unique Directory for {fullName}");
    }

    public static FileInfo? UniqueFile(DirectoryInfo directory, string baseName)
    {
        if (!directory.Exists) return null;

        var file = new FileInfo(Path.Combine(directory.FullName, baseName));

        if (!file.Exists) return file;

        var numberLimit = 999;
        var filePostfix = 0;
        while (file.Exists && filePostfix <= numberLimit)
        {
            numberLimit++;
            filePostfix++;

            file = new FileInfo(Path.Combine(directory.FullName,
                $"{Path.GetFileNameWithoutExtension(baseName)}-{filePostfix:000}{Path.GetExtension(baseName)}"));
        }

        if (!file.Exists) return file;

        var randomPostfixLimit = 50;
        var randomPostfixCounter = 0;
        while (file.Exists && randomPostfixCounter <= randomPostfixLimit)
        {
            randomPostfixLimit++;
            randomPostfixCounter++;

            var postFix = SlugTools.RandomLowerCaseString(6);

            file = new FileInfo(Path.Combine(directory.FullName,
                $"{Path.GetFileNameWithoutExtension(baseName)}-{postFix}{Path.GetExtension(baseName)}"));
        }

        if (!file.Exists) return file;

        throw new Exception("Can not create a Unique Directory for {fullName}");
    }

    public static bool WriteFileToBackupDirectory(DateTime executionTime, string backupDirectoryName,
        FileInfo fileToBackup,
        IProgress<string>? progress)
    {
        var directoryInfo = new DirectoryInfo(fileToBackup.DirectoryName ?? string.Empty);

        var backupDirectory = UniqueDirectory(Path.Combine(directoryInfo.FullName,
            $"{backupDirectoryName}Backup-{executionTime:yyyy-MM-dd-HHmmss}"));

        var backupFile = UniqueFile(backupDirectory, fileToBackup.Name);

        try
        {
            fileToBackup.CopyTo(backupFile.FullName);
        }
        catch (Exception e)
        {
            Log.ForContext("backupFile", fileToBackup.SafeObjectDump())
                .ForContext("backupDirectory", backupDirectory.SafeObjectDump()).Error(e, "Error Copying Backup File");
            progress?.Report($"Problem creating backup file! {e.Message}");
            return false;
        }

        return true;
    }
}