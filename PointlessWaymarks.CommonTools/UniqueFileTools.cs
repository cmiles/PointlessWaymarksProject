using Serilog;

namespace PointlessWaymarks.CommonTools;

public static class UniqueFileTools
{
    /// <summary>
    ///     Performs a FileInfo.MoveTo after establishing a unique name for the file in the destination
    ///     directory.
    /// </summary>
    /// <param name="originalFile"></param>
    /// <param name="moveToFullName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    public static FileInfo MoveToWithUniqueName(this FileInfo originalFile, string moveToFullName)
    {
        var toMoveTo = new FileInfo(moveToFullName);

        if (toMoveTo.Directory == null)
            throw new ArgumentException(
                $"DirectoryInfo for the Destination {moveToFullName} can not be null - original File {originalFile.FullName}");

        var uniqueFile = UniqueFile(toMoveTo.Directory, toMoveTo.Name);

        if (uniqueFile == null)
            throw new Exception(
                $"Error finding a unique name when moving {originalFile.FullName} to {moveToFullName}.");

        originalFile.MoveTo(uniqueFile.FullName);

        return originalFile;
    }

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

    public static DirectoryInfo UniqueRandomLetterNameDirectory(string parentDirectory, int nameLength)
    {
        var parent = new DirectoryInfo(parentDirectory);

        if (!parent.Exists) parent.Create();

        for (var i = 0; i < 1000; i++)
        {
            var random = SlugTools.RandomLowerCaseString(nameLength);
            if (!Directory.Exists(Path.Combine(parent.FullName, random)))
            {
                var toReturn = new DirectoryInfo(Path.Combine(parent.FullName, random));
                toReturn.Create();
                toReturn.Refresh();
                return toReturn;
            }
        }

        throw new Exception($"Can not create a Unique Directory for {parentDirectory}");
    }

    public static bool WriteFileToDefaultStorageDirectoryBackupDirectory(DateTime executionTime,
        string backupDirectoryName,
        FileInfo fileToBackup,
        IProgress<string>? progress)
    {
        var directoryInfo = FileLocationTools.DefaultStorageDirectory();

        var backupDirectory = new DirectoryInfo(Path.Combine(directoryInfo.FullName,
            $"{backupDirectoryName}Backup-{executionTime:yyyy-MM-dd-HHmmss}"));

        if (!backupDirectory.Exists) backupDirectory.Create();

        var backupFile = UniqueFile(backupDirectory, fileToBackup.Name);

        try
        {
            fileToBackup.CopyTo(backupFile!.FullName);
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

    public static bool WriteFileToInPlaceBackupDirectory(DateTime executionTime, string backupDirectoryName,
        FileInfo fileToBackup,
        IProgress<string>? progress)
    {
        var directoryInfo = new DirectoryInfo(fileToBackup.DirectoryName ?? string.Empty);

        var backupDirectory = new DirectoryInfo(Path.Combine(directoryInfo.FullName,
            $"{backupDirectoryName}Backup-{executionTime:yyyy-MM-dd-HHmmss}"));

        if (!backupDirectory.Exists) backupDirectory.Create();

        var backupFile = UniqueFile(backupDirectory, fileToBackup.Name);

        try
        {
            fileToBackup.CopyTo(backupFile!.FullName);
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