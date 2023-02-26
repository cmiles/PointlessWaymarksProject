﻿using System.Text.RegularExpressions;
using Serilog;

namespace PointlessWaymarks.CommonTools;

public static class FileAndFolderTools
{
    /// <summary>
    ///     Appends the Long File Prefix \\?\ to the FileInfo FullName - this should only be used to interop in situations
    ///     not related to .NET Core - .NET Core handles this automatically. Only tested with absolute file paths.
    /// </summary>
    /// <param name="forName"></param>
    /// <returns></returns>
    public static string FullNameWithLongFilePrefix(this FileInfo? forName)
    {
        //See https://stackoverflow.com/questions/5188527/how-to-deal-with-files-with-a-name-longer-than-259-characters for a good summary
        //and for some library and other alternatives.

        return forName.FullName.Length > 240 ? $"\\\\?\\{forName.FullName}" : forName.FullName;
    }

    /// <summary>
    ///     Appends the Long File Prefix \\?\ to the FileInfo FullName - this should only be used to interop in situations
    ///     not related to .NET Core - .NET Core handles this automatically. Only tested with absolute file paths.
    /// </summary>
    /// <param name="forName"></param>
    /// <returns></returns>
    public static string? FullNameWithLongFilePrefixForPossibleNull(this FileInfo? forName)
    {
        //See https://stackoverflow.com/questions/5188527/how-to-deal-with-files-with-a-name-longer-than-259-characters for a good summary
        //and for some library and other alternatives.
        if (forName == null) return null;

        return forName.FullName.Length > 240 ? $"\\\\?\\{forName.FullName}" : forName.FullName;
    }

    public static string InvalidFileNameCharsRegexPattern()
    {
        return $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]";
    }

    /// <summary>
    ///     This checks if a string is 'no url encoding needed'
    /// </summary>
    /// <param name="testName"></param>
    /// <returns></returns>
    public static bool IsNoUrlEncodingNeeded(string testName)
    {
        if (string.IsNullOrWhiteSpace(testName)) return false;

        return Regex.IsMatch(testName, @"^[a-zA-Z\d_\-]+$");
    }

    /// <summary>
    ///     This checks if a string is 'no url encoding needed'
    /// </summary>
    /// <param name="testName"></param>
    /// <returns></returns>
    public static bool IsNoUrlEncodingNeededLowerCase(string testName)
    {
        if (string.IsNullOrWhiteSpace(testName)) return false;

        return Regex.IsMatch(testName, @"^[a-z\d_\-]+$");
    }

    /// <summary>
    ///     This checks if a string is 'no url encoding needed' with the exception of spaces which are allowed
    /// </summary>
    /// <param name="testName"></param>
    /// <returns></returns>
    public static bool IsNoUrlEncodingNeededLowerCaseSpacesOk(string testName)
    {
        if (string.IsNullOrWhiteSpace(testName)) return false;

        return Regex.IsMatch(testName, @"^[a-z \d_\-]+$");
    }

    public static bool IsValidWindowsFileSystemFilename(string testName)
    {
        //https://stackoverflow.com/questions/62771/how-do-i-check-if-a-given-string-is-a-legal-valid-file-name-under-windows
        var containsABadCharacter = new Regex(InvalidFileNameCharsRegexPattern());
        if (containsABadCharacter.IsMatch(testName)) return false;

        return true;
    }

    public static bool PictureFileTypeIsSupported(FileInfo? toCheck)
    {
        if (!toCheck.Exists) return false;

        return toCheck.Extension.ToUpperInvariant().Contains("JPG") ||
               toCheck.Extension.ToUpperInvariant().Contains("JPEG");
    }

    /// <summary>
    ///     Reads all text via a stream opened with FileShare.ReadWrite and FileAccess.Read
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static string ReadAllText(string file)
    {
        using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var textReader = new StreamReader(fileStream);
        return textReader.ReadToEnd();
    }

    /// <summary>
    /// If the file exists and is valid for the conventions of this program the original file is returned.
    /// If the file is not valid the program attempts to auto-fix the name - the renamed version will be
    /// a copy of the original (the original will not be moved or modified).
    /// </summary>
    /// <param name="selectedFile"></param>
    /// <returns></returns>
    public static async Task<FileInfo?> TryAutoCleanRenameFileForProgramConventions(FileInfo? selectedFile)
    {
        if (selectedFile is not { Exists: true }) return null;

        //No rename needed
        if (IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(selectedFile.Name))) return selectedFile;

        var cleanedFileNamePath = Path.GetFileNameWithoutExtension(selectedFile.Name).TrimNullToEmpty();

        return await TryAutoRenameFileForProgramConventions(selectedFile, cleanedFileNamePath);
    }

    /// <summary>
    /// Tries to rename a file to an automatically cleaned version of the input suggested name. The
    /// renamed file will be a copy of the original leaving the original in place.
    /// </summary>
    /// <param name="selectedFile"></param>
    /// <param name="suggestedName"></param>
    /// <returns></returns>
    public static Task<FileInfo?> TryAutoRenameFileForProgramConventions(FileInfo? selectedFile, string suggestedName)
    {
        if (selectedFile is not { Exists: true }) return Task.FromResult<FileInfo?>(null);

        var cleanedName = SlugTools.CreateSlug(false, suggestedName.TrimNullToEmpty());

        if (string.IsNullOrWhiteSpace(cleanedName)) return Task.FromResult<FileInfo?>(null);

        var moveToDirectory = selectedFile.Directory!;
        var baseMoveToName = $"{cleanedName}{Path.GetExtension(selectedFile.Name)}";

        if(baseMoveToName == selectedFile.Name) return Task.FromResult(selectedFile);

        var moveToName = UniqueFileTools.UniqueFile(moveToDirectory, baseMoveToName)!.FullName;

        try
        {
            File.Copy(selectedFile.FullName, moveToName);
        }
        catch (Exception e)
        {
            Log.ForContext("selectedFile", selectedFile).ForContext("suggestedName", suggestedName)
                .Error(e, "Exception while trying to rename file");
            return Task.FromResult<FileInfo?>(null);
        }

        var finalFile = new FileInfo(moveToName);

        if (!finalFile.Exists)
        {
            Log.ForContext("selectedFile", selectedFile).ForContext("suggestedName", suggestedName)
                .Error("Unknown error renaming file - original file still selected.");
            return Task.FromResult<FileInfo?>(null);
        }

        return Task.FromResult(finalFile);
    }

    public static string TryMakeFilenameValid(string filenameWithoutExtensionToTransform)
    {
        return string.IsNullOrWhiteSpace(filenameWithoutExtensionToTransform)
            ? string.Empty
            : Regex.Replace(filenameWithoutExtensionToTransform, InvalidFileNameCharsRegexPattern(), string.Empty);
    }
}