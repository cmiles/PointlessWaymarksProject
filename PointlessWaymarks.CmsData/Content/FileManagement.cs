using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using Polly;
using Serilog;

namespace PointlessWaymarks.CmsData.Content;

public static class FileManagement
{
    public static async Task<List<GenerationReturn>> CheckContentFolderStructure(this UserSettings settings)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var returnList = new List<GenerationReturn>();

        //!!Content List

        returnList.AddRange((await db.FileContents.ToListAsync().ConfigureAwait(false)).Select(x =>
            GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSiteFileContentDirectory(x),
                $"Check Content Folder for File {x.Title}")));

        returnList.AddRange((await db.GeoJsonContents.ToListAsync().ConfigureAwait(false)).Select(x =>
            GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSiteGeoJsonContentDirectory(x),
                $"Check Content Folder for GeoJson {x.Title}")));

        returnList.AddRange((await db.ImageContents.ToListAsync().ConfigureAwait(false)).Select(x =>
            GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSiteImageContentDirectory(x),
                $"Check Content Folder for Image {x.Title}")));

        returnList.AddRange((await db.LineContents.ToListAsync().ConfigureAwait(false)).Select(x =>
            GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSiteLineContentDirectory(x),
                $"Check Content Folder for Line {x.Title}")));

        returnList.AddRange((await db.NoteContents.ToListAsync().ConfigureAwait(false)).Select(x =>
            GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSiteNoteContentDirectory(x),
                $"Check Content Folder for Note {x.Title}")));

        returnList.AddRange((await db.PhotoContents.ToListAsync().ConfigureAwait(false)).Select(x =>
            GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSitePhotoContentDirectory(x),
                $"Check Content Folder for Photo {x.Title}")));

        returnList.AddRange((await db.PointContents.ToListAsync().ConfigureAwait(false)).Select(x =>
            GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSitePointContentDirectory(x),
                $"Check Content Folder for Point {x.Title}")));

        returnList.AddRange((await db.PostContents.ToListAsync().ConfigureAwait(false)).Select(x =>
            GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSitePostContentDirectory(x),
                $"Check Content Folder for Post {x.Title}")));

        returnList.AddRange((await db.VideoContents.ToListAsync().ConfigureAwait(false)).Select(x =>
            GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSiteVideoContentDirectory(x),
                $"Check Content Folder for Video {x.Title}")));

        return returnList;
    }

    public static async Task<GenerationReturn> CheckFileOriginalFileIsInMediaAndContentDirectories(
        FileContent? dbContent)
    {
        if (dbContent == null)
            return GenerationReturn.Error(
                "Null File Content was submitted to the Check of File in the Media and Content Directories");

        UserSettingsSingleton.CurrentSettings().VerifyOrCreateAllTopLevelFolders();

        if (string.IsNullOrWhiteSpace(dbContent.OriginalFileName))
            return GenerationReturn.Error($"File {dbContent.Title} does not have an Original File assigned",
                dbContent.ContentId);

        var archiveFile = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileDirectory().FullName,
            dbContent.OriginalFileName));

        var fileContentDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(dbContent);

        var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, dbContent.OriginalFileName));

        if (!archiveFile.Exists && !contentFile.Exists)
            return GenerationReturn.Error(
                $"Neither {archiveFile.FullName} nor {contentFile.FullName} exists - " +
                $"there appears to be a file missing for File Titled {dbContent.Title} " + $"slug {dbContent.Slug}",
                dbContent.ContentId);

        if (archiveFile.Exists && !contentFile.Exists)
            await archiveFile.CopyToAndLogAsync(contentFile.FullName).ConfigureAwait(false);

        if (!archiveFile.Exists && contentFile.Exists)
            await contentFile.CopyToAndLogAsync(archiveFile.FullName).ConfigureAwait(false);

        archiveFile.Refresh();
        contentFile.Refresh();

        var bothFilesPresent = archiveFile.Exists && contentFile.Exists;

        if (bothFilesPresent)
            return GenerationReturn.Success($"File {dbContent.Title} Present in both Content and Media Folders",
                dbContent.ContentId);

        return GenerationReturn.Error(
            $"There was a problem - Archive File Present: {archiveFile.Exists}, " +
            $"Content File Present {contentFile.Exists} - {archiveFile.FullName}; {contentFile.FullName}",
            dbContent.ContentId);
    }

    public static async Task<GenerationReturn> CheckImageFileIsInMediaAndContentDirectories(ImageContent? dbContent)
    {
        if (dbContent == null)
            return GenerationReturn.Error(
                "Null Image Content was submitted to the Check of File in the Media and Content Directories");

        UserSettingsSingleton.CurrentSettings().VerifyOrCreateAllTopLevelFolders();

        if (string.IsNullOrWhiteSpace(dbContent.OriginalFileName))
            return GenerationReturn.Error($"Image {dbContent.Title} does not have an Original File assigned",
                dbContent.ContentId);

        var archiveFile = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageDirectory().FullName,
            dbContent.OriginalFileName));

        var fileContentDirectory =
            UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(dbContent);

        var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, dbContent.OriginalFileName));

        if (!archiveFile.Exists && !contentFile.Exists)
            return GenerationReturn.Error(
                $"Neither {archiveFile.FullName} nor {contentFile.FullName} exists - " +
                $"there appears to be a file missing for Image Title {dbContent.Title} " + $"slug {dbContent.Slug}",
                dbContent.ContentId);


        if (archiveFile.Exists && !contentFile.Exists)
            await archiveFile.CopyToAndLogAsync(contentFile.FullName).ConfigureAwait(false);

        if (!archiveFile.Exists && contentFile.Exists)
            await contentFile.CopyToAndLogAsync(archiveFile.FullName).ConfigureAwait(false);

        archiveFile.Refresh();
        contentFile.Refresh();

        var bothFilesPresent = archiveFile.Exists && contentFile.Exists;

        if (bothFilesPresent)
            return GenerationReturn.Success($"Image {dbContent.Title} Present in both Content and Media Folders",
                dbContent.ContentId);

        return GenerationReturn.Error(
            $"There was a problem - Archive Image Present: {archiveFile.Exists}, " +
            $"Content Image Present {contentFile.Exists} - {archiveFile.FullName}; {contentFile.FullName}",
            dbContent.ContentId);
    }

    public static async Task<GenerationReturn> CheckPhotoFileIsInMediaAndContentDirectories(PhotoContent? dbContent)
    {
        if (dbContent == null)
            return GenerationReturn.Error(
                "Null Photo Content was submitted to the Check of File in the Media and Content Directories");

        UserSettingsSingleton.CurrentSettings().VerifyOrCreateAllTopLevelFolders();

        if (string.IsNullOrWhiteSpace(dbContent.OriginalFileName))
            return GenerationReturn.Error($"Photo {dbContent.Title} does not have an Original File assigned",
                dbContent.ContentId);

        var archiveFile = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoDirectory().FullName,
            dbContent.OriginalFileName));

        var fileContentDirectory =
            UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(dbContent);

        var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, dbContent.OriginalFileName));

        if (!archiveFile.Exists && !contentFile.Exists)
            return GenerationReturn.Error(
                $"Neither {archiveFile.FullName} nor {contentFile.FullName} exists - " +
                $"there appears to be a file missing for Photo Title {dbContent.Title} " + $"slug {dbContent.Slug}",
                dbContent.ContentId);

        if (archiveFile.Exists && !contentFile.Exists)
            await archiveFile.CopyToAndLogAsync(contentFile.FullName).ConfigureAwait(false);

        if (!archiveFile.Exists && contentFile.Exists)
            await contentFile.CopyToAndLogAsync(archiveFile.FullName).ConfigureAwait(false);

        archiveFile.Refresh();
        contentFile.Refresh();

        var bothFilesPresent = archiveFile.Exists && contentFile.Exists;

        if (bothFilesPresent)
            return GenerationReturn.Success($"Photo {dbContent.Title} Present in both Content and Media Folders",
                dbContent.ContentId);

        return GenerationReturn.Error(
            $"There was a problem - Archive Photo Present: {archiveFile.Exists}, " +
            $"Content Photo Present {contentFile.Exists} - {archiveFile.FullName}; {contentFile.FullName}",
            dbContent.ContentId);
    }

    public static async Task<GenerationReturn> CheckVideoOriginalFileIsInMediaAndContentDirectories(
        VideoContent? dbContent)
    {
        if (dbContent == null)
            return GenerationReturn.Error(
                "Null Video Content was submitted to the Check of File in the Media and Content Directories");

        UserSettingsSingleton.CurrentSettings().VerifyOrCreateAllTopLevelFolders();

        if (string.IsNullOrWhiteSpace(dbContent.OriginalFileName))
            return GenerationReturn.Error($"Video {dbContent.Title} does not have an Original File assigned",
                dbContent.ContentId);

        var archiveFile = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveVideoDirectory().FullName,
            dbContent.OriginalFileName));

        var fileContentDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteVideoContentDirectory(dbContent);

        var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, dbContent.OriginalFileName));

        if (!archiveFile.Exists && !contentFile.Exists)
            return GenerationReturn.Error(
                $"Neither {archiveFile.FullName} nor {contentFile.FullName} exists - " +
                $"there appears to be a file missing for the Video Titled {dbContent.Title} " +
                $"slug {dbContent.Slug}",
                dbContent.ContentId);

        if (archiveFile.Exists && !contentFile.Exists)
            await archiveFile.CopyToAndLogAsync(contentFile.FullName).ConfigureAwait(false);

        if (!archiveFile.Exists && contentFile.Exists)
            await contentFile.CopyToAndLogAsync(archiveFile.FullName).ConfigureAwait(false);

        archiveFile.Refresh();
        contentFile.Refresh();

        var bothFilesPresent = archiveFile.Exists && contentFile.Exists;

        if (bothFilesPresent)
            return GenerationReturn.Success($"Video {dbContent.Title} Present in both Content and Media Folders",
                dbContent.ContentId);

        return GenerationReturn.Error(
            $"There was a problem - Archive Video Present: {archiveFile.Exists}, " +
            $"Content Video Present {contentFile.Exists} - {archiveFile.FullName}; {contentFile.FullName}",
            dbContent.ContentId);
    }

    public static async Task<List<GenerationReturn>> CleanAndResizeAllImageFiles(IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.ImageContents.ToListAsync().ConfigureAwait(false);

        var loopCount = 1;
        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Images to Clean and Resize");

        var returnList = new List<GenerationReturn>();

        foreach (var loopItem in allItems)
        {
            progress?.Report($"Image Clean and Resize for {loopItem.Title} - {loopCount} of {totalCount}");

            returnList.Add(await PictureResizing.CopyCleanResizeImage(loopItem, progress).ConfigureAwait(false));

            loopCount++;
        }

        return returnList;
    }

    public static async Task<List<GenerationReturn>> CleanAndResizeAllPhotoFiles(IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.PhotoContents.ToListAsync().ConfigureAwait(false);

        var loopCount = 1;
        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Photos to Clean and Resize");

        var returnList = new List<GenerationReturn>();

        foreach (var loopItem in allItems)
        {
            progress?.Report($"Photo Clean and Resize for {loopItem.Title} - {loopCount} of {totalCount}");

            returnList.Add(await PictureResizing.CopyCleanResizePhoto(loopItem, progress).ConfigureAwait(false));

            loopCount++;
        }

        return returnList;
    }

    public static void CleanUpTemporaryFiles()
    {
        var temporaryDirectory = FileLocationTools.TempStorageDirectory();

        var allFiles = temporaryDirectory.GetFiles().ToList();

        var frozenUtcNow = DateTime.UtcNow;

        foreach (var loopFiles in allFiles)
            try
            {
                var creationDayDiff = frozenUtcNow.Subtract(loopFiles.CreationTimeUtc).Days;
                var lastWriteDayDiff = frozenUtcNow.Subtract(loopFiles.LastWriteTimeUtc).Days;

                if (creationDayDiff > 28 && lastWriteDayDiff > 28)
                    loopFiles.Delete();
            }
            catch (Exception e)
            {
                Log.Error(e, "FileManagement.CleanUpTemporaryFiles - could not delete temporary file.");
            }
    }

    public static void CleanupTemporaryHtmlFiles()
    {
        var temporaryDirectory = FileLocationTools.TempStorageHtmlDirectory();

        var allFiles = temporaryDirectory.GetFiles().ToList();

        var frozenUtcNow = DateTime.UtcNow;

        foreach (var loopFiles in allFiles)
            try
            {
                var creationDayDiff = frozenUtcNow.Subtract(loopFiles.CreationTimeUtc).Days;
                var lastAccessDayDiff = frozenUtcNow.Subtract(loopFiles.LastAccessTimeUtc).TotalDays;
                var lastWriteDayDiff = frozenUtcNow.Subtract(loopFiles.LastWriteTimeUtc).TotalDays;

                if (creationDayDiff > 2 && lastAccessDayDiff > 2 && lastWriteDayDiff > 2)
                    loopFiles.Delete();
            }
            catch (Exception e)
            {
                Log.ForContext(nameof(loopFiles), loopFiles.SafeObjectDump()).Error(e,
                    "FileManagement.CleanUpTemporaryFiles - could not delete temporary file.");
            }

        var allDirectories = temporaryDirectory.GetDirectories();

        foreach (var loopDirectories in allDirectories)
            try
            {
                var creationDayDiff = frozenUtcNow.Subtract(loopDirectories.CreationTimeUtc).TotalDays;
                var lastWriteDayDiff = frozenUtcNow.Subtract(loopDirectories.LastWriteTimeUtc).TotalDays;

                if (creationDayDiff > 6 && lastWriteDayDiff > 6)
                {
                    Log.Information("Html Temp Directory - Deleting Directory {0}", loopDirectories.FullName);
                    loopDirectories.Delete(true);
                }
            }
            catch (Exception e)
            {
                Log.ForContext(nameof(loopDirectories), loopDirectories.SafeObjectDump()).Error(e,
                    "FileManagement.CleanUpTemporaryFiles - could not delete temporary file.");
            }
    }

    public static async Task<List<GenerationReturn>> ConfirmAllFileContentFilesArePresent(
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.FileContents.Where(x => string.IsNullOrEmpty(x.OriginalFileName)).ToListAsync()
            .ConfigureAwait(false);

        var loopCount = 1;
        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Files to Check");

        var returnList = new List<GenerationReturn>();

        foreach (var loopItem in allItems)
        {
            progress?.Report($"File Check for {loopItem.Title} - {loopCount} of {totalCount}");

            returnList.Add(await CheckFileOriginalFileIsInMediaAndContentDirectories(loopItem).ConfigureAwait(false));

            loopCount++;
        }

        return returnList;
    }

    public static async Task<FileInfo> CopyToAndLog(this FileInfo fileInfo, string destinationFileName)
    {
        var returnValue = fileInfo.CopyTo(destinationFileName);

        await LogFileWriteAsync(destinationFileName).ConfigureAwait(false);

        return returnValue;
    }

    public static async Task<FileInfo> CopyToAndLogAsync(this FileInfo fileInfo, string destinationFileName)
    {
        var returnValue = fileInfo.CopyTo(destinationFileName);

        await LogFileWriteAsync(destinationFileName).ConfigureAwait(false);

        return returnValue;
    }

    public static async Task LogFileWriteAsync(string fileName)
    {
        await Policy.Handle<Exception>().WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4)
            },
            (_, _, retryCount, _) =>
                Log.Debug("Sqlite Db Retry - LogFileWriteAsync {fileName}, Retry Count {retryCount}",
                    fileName,
                    retryCount)).ExecuteAsync(async () =>
        {
            var db = await Db.Context();

            await db.GenerationFileWriteLogs.AddAsync(new GenerationFileWriteLog
            {
                FileName = fileName, WrittenOnVersion = DateTime.Now.TrimDateTimeToSeconds().ToUniversalTime()
            });
            await db.SaveChangesAsync(true);
        }).ConfigureAwait(false);
    }

    public static async Task MoveFileAndLog(string sourceFile, string destinationFile)
    {
        File.Move(sourceFile, destinationFile);

        await LogFileWriteAsync(destinationFile).ConfigureAwait(false);
    }

    public static async Task MoveFileAndLogAsync(string sourceFile, string destinationFile)
    {
        File.Move(sourceFile, destinationFile);

        await LogFileWriteAsync(destinationFile).ConfigureAwait(false);
    }

    public static async Task RemoveContentDataJsonFilesNotInCurrentDb(IProgress<string>? progress)
    {
        progress?.Report("ContentData Directory Cleanup - Starting Queries for All Database Content Ids");

        var db = await Db.Context().ConfigureAwait(false);
        var allContentIds = new List<Guid>();

        allContentIds.AddRange(await db.FileContents.Select(x => x.ContentId).ToListAsync().ConfigureAwait(false));
        allContentIds.AddRange(await db.GeoJsonContents.Select(x => x.ContentId).ToListAsync().ConfigureAwait(false));
        allContentIds.AddRange(await db.ImageContents.Select(x => x.ContentId).ToListAsync().ConfigureAwait(false));
        allContentIds.AddRange(await db.LineContents.Select(x => x.ContentId).ToListAsync().ConfigureAwait(false));
        allContentIds.AddRange(await db.MapComponents.Select(x => x.ContentId).ToListAsync().ConfigureAwait(false));
        allContentIds.AddRange(await db.NoteContents.Select(x => x.ContentId).ToListAsync().ConfigureAwait(false));
        allContentIds.AddRange(await db.PhotoContents.Select(x => x.ContentId).ToListAsync().ConfigureAwait(false));
        allContentIds.AddRange(await db.PointContents.Select(x => x.ContentId).ToListAsync().ConfigureAwait(false));
        allContentIds.AddRange(await db.PostContents.Select(x => x.ContentId).ToListAsync().ConfigureAwait(false));
        allContentIds.AddRange(await db.VideoContents.Select(x => x.ContentId).ToListAsync().ConfigureAwait(false));

        progress?.Report(
            $"ContentData Directory Cleanup - found {allContentIds.Count} Content Ids in the Database - Getting Files");

        var allContentDataFiles = UserSettingsSingleton.CurrentSettings().LocalSiteContentDataDirectory()
            .GetFiles("*.json").ToList();

        progress?.Report($"ContentData Directory Cleanup - found {allContentDataFiles.Count} Files to Check");

        var fileCount = 0;

        foreach (var loopFiles in allContentDataFiles)
        {
            fileCount++;

            if (fileCount % 500 == 0)
                progress?.Report(
                    $"ContentData Directory Cleanup - Checking Existing File {fileCount} of {allContentDataFiles.Count}");

            var contentId = Path.GetFileNameWithoutExtension(loopFiles.Name);
            if (Guid.TryParse(contentId, out var parsedGuid))
                if (!allContentIds.Contains(parsedGuid))
                    try
                    {
                        loopFiles.Delete();
                        progress?.Report($"Deleted {loopFiles.FullName}");
                    }
                    catch (Exception e)
                    {
                        Log.ForContext("exception", e.SafeObjectDump()).Debug(
                            "Error Deleting {file} from the ContentData Directory - Error Ignored",
                            loopFiles.FullName);
                    }
        }
    }

    public static async Task RemoveContentDirectoriesAndFilesNotFoundInCurrentDatabase(
        IProgress<string>? progress = null)
    {
        //!!Content List
        await RemoveFileDirectoriesNotFoundInCurrentDatabase(progress).ConfigureAwait(false);
        await RemoveGeoJsonDirectoriesNotFoundInCurrentDatabase(progress).ConfigureAwait(false);
        await RemoveImageDirectoriesNotFoundInCurrentDatabase(progress).ConfigureAwait(false);
        await RemoveLineDirectoriesNotFoundInCurrentDatabase(progress).ConfigureAwait(false);
        await RemoveMapHistoryDataDirectoryFilesNotInCurrentDb(progress).ConfigureAwait(false);
        await RemoveNoteDirectoriesNotFoundInCurrentDatabase(progress).ConfigureAwait(false);
        await RemovePhotoDirectoriesNotFoundInCurrentDatabase(progress).ConfigureAwait(false);
        await RemovePointDirectoriesNotFoundInCurrentDatabase(progress).ConfigureAwait(false);
        await RemovePostDirectoriesNotFoundInCurrentDatabase(progress).ConfigureAwait(false);
        await RemoveVideoDirectoriesNotFoundInCurrentDatabase(progress).ConfigureAwait(false);
        await RemoveContentDataJsonFilesNotInCurrentDb(progress).ConfigureAwait(false);
        await RemoveGpxFilesNotInCurrentDb(progress).ConfigureAwait(false);
        await RemoveTagContentFilesNotInCurrentDatabase(progress).ConfigureAwait(false);
    }

    public static async Task RemoveFileDirectoriesNotFoundInCurrentDatabase(IProgress<string>? progress)
    {
        progress?.Report("Starting Directory Cleanup");

        var db = await Db.Context().ConfigureAwait(false);
        var dbFolders = db.FileContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

        var siteTopLevelFileDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteFileDirectory();

        var firstLevelFolders = siteTopLevelFileDirectory.GetDirectories().OrderBy(x => x.Name).ToList();

        List<DirectoryInfo> folderDirectories = [];

        foreach (var loopFoldersDirectoryInfo in firstLevelFolders)
            if (!dbFolders.Contains(loopFoldersDirectoryInfo.Name))
                loopFoldersDirectoryInfo.Delete(true);
            else
                folderDirectories.Add(loopFoldersDirectoryInfo);

        progress?.Report(
            $"Found {folderDirectories.Count} Existing File Directories to Check against {dbFolders.Count} File Folders in the Database");

        foreach (var loopExistingDirectories in folderDirectories)
        {
            if (!dbFolders.Contains(loopExistingDirectories.Name))
            {
                progress?.Report($"Deleting {loopExistingDirectories.FullName}");

                loopExistingDirectories.Delete(true);
                continue;
            }

            progress?.Report($"Staring File Content Directory Check for {loopExistingDirectories.FullName}");

            var existingContentDirectories = loopExistingDirectories.GetDirectories().OrderBy(x => x.Name).ToList();
            var dbContentSlugs = db.FileContents.Where(x => x.Folder == loopExistingDirectories.Name)
                .Select(x => x.Slug).OrderBy(x => x).ToList();

            progress?.Report(
                $"Found {existingContentDirectories.Count} Existing File Content Directories in {loopExistingDirectories.Name} to Check against {dbContentSlugs.Count} Content Items in the Database");

            foreach (var loopExistingContentDirectories in existingContentDirectories)
            {
                if (!dbContentSlugs.Contains(loopExistingContentDirectories.Name))
                {
                    progress?.Report($"Deleting {loopExistingContentDirectories.FullName}");
                    loopExistingContentDirectories.Delete(true);
                    continue;
                }

                progress?.Report($"{loopExistingContentDirectories.FullName} matches current File Content");
            }
        }

        var allContent = await db.FileContents.ToListAsync();

        foreach (var loopFileContent in allContent)
        {
            var folder = UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(loopFileContent, false);

            if (!folder.Exists) continue;

            var fileList = folder.GetFiles().ToList();
            fileList.RemoveAll(x =>
                x.Name.StartsWith(folder.Name, StringComparison.OrdinalIgnoreCase));
            fileList.RemoveAll(x => x.Name.StartsWith("Historic", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(loopFileContent.OriginalFileName))
                fileList.RemoveAll(x =>
                    x.Name.Equals(loopFileContent.OriginalFileName, StringComparison.OrdinalIgnoreCase));

            if (fileList.Any())
                progress?.Report(
                    $"Deleting Files in {folder.FullName} - {string.Join(", ", fileList.OrderBy(x => x.Name).Select(x => x.Name))}");

            foreach (var loopFiles in fileList)
                try
                {
                    loopFiles.Delete();
                }
                catch (Exception e)
                {
                    Log.ForContext("exception", e.SafeObjectDump())
                        .Debug("Error Deleting {file} - Exception Ignored", loopFiles);
                }
        }

        progress?.Report("Ending File Directory Cleanup");
    }

    public static async Task RemoveFileMediaArchiveFilesNotInCurrentDatabase(IProgress<string>? progress)
    {
        progress?.Report("Starting File Media Archive Cleanup");

        var db = await Db.Context().ConfigureAwait(false);
        var siteFileMediaArchiveDirectory =
            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileDirectory();
        var siteFileMediaArchiveFiles = siteFileMediaArchiveDirectory.GetFiles().OrderBy(x => x.Name).ToList();

        var dbNames = db.FileContents.Select(x => x.OriginalFileName).OrderBy(x => x).ToList();

        progress?.Report(
            $"Found {siteFileMediaArchiveFiles.Count} Existing File Files in the Media Archive - Checking against {dbNames.Count} File Names  in the Database");

        foreach (var loopFiles in siteFileMediaArchiveFiles)
        {
            if (!dbNames.Contains(loopFiles.Name))
            {
                progress?.Report($"Deleting {loopFiles.Name}");
                loopFiles.Delete();
                continue;
            }

            progress?.Report($"Found {loopFiles.Name} in Database");
        }
    }

    public static async Task RemoveGeoJsonDirectoriesNotFoundInCurrentDatabase(IProgress<string>? progress)
    {
        progress?.Report("Starting Directory Cleanup");

        var db = await Db.Context().ConfigureAwait(false);
        var dbFolders = db.GeoJsonContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

        var siteTopLevelGeoJsonDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteGeoJsonDirectory();

        var firstLevelFolders = siteTopLevelGeoJsonDirectory.GetDirectories().OrderBy(x => x.Name).ToList();

        List<DirectoryInfo> folderDirectories = [];

        foreach (var loopFoldersDirectoryInfo in firstLevelFolders)
            if (!dbFolders.Contains(loopFoldersDirectoryInfo.Name))
                loopFoldersDirectoryInfo.Delete(true);
            else
                folderDirectories.Add(loopFoldersDirectoryInfo);

        progress?.Report(
            $"Found {folderDirectories.Count} Existing GeoJson Directories to Check against {dbFolders.Count} GeoJson Folders in the Database");

        foreach (var loopExistingDirectories in folderDirectories)
        {
            if (!dbFolders.Contains(loopExistingDirectories.Name))
            {
                progress?.Report($"Deleting {loopExistingDirectories.FullName}");

                loopExistingDirectories.Delete(true);
                continue;
            }

            progress?.Report($"Staring GeoJson Content Directory Check for {loopExistingDirectories.FullName}");

            var existingContentDirectories = loopExistingDirectories.GetDirectories().OrderBy(x => x.Name).ToList();
            var dbContentSlugs = db.GeoJsonContents.Where(x => x.Folder == loopExistingDirectories.Name)
                .Select(x => x.Slug).OrderBy(x => x).ToList();

            progress?.Report(
                $"Found {existingContentDirectories.Count} Existing GeoJson Content Directories in {loopExistingDirectories.Name} to Check against {dbContentSlugs.Count} Content Items in the Database");

            foreach (var loopExistingContentDirectories in existingContentDirectories)
            {
                if (!dbContentSlugs.Contains(loopExistingContentDirectories.Name))
                {
                    progress?.Report($"Deleting {loopExistingContentDirectories.FullName}");
                    loopExistingContentDirectories.Delete(true);
                    continue;
                }

                var fileList = loopExistingContentDirectories.GetFiles().ToList();
                fileList.RemoveAll(x =>
                    x.Name.StartsWith(loopExistingContentDirectories.Name, StringComparison.OrdinalIgnoreCase));
                fileList.RemoveAll(x => x.Name.StartsWith("Historic", StringComparison.OrdinalIgnoreCase));

                if (fileList.Any())
                    progress?.Report(
                        $"Deleting Files in {loopExistingContentDirectories.FullName} - {string.Join(", ", fileList.OrderBy(x => x.Name).Select(x => x.Name))}");

                foreach (var loopFiles in fileList)
                    try
                    {
                        loopFiles.Delete();
                    }
                    catch (Exception e)
                    {
                        Log.ForContext("exception", e.SafeObjectDump())
                            .Debug("Error Deleting {file} - Exception Ignored", loopFiles);
                    }

                progress?.Report($"{loopExistingContentDirectories.FullName} matches current GeoJson Content");
            }
        }

        progress?.Report("Ending GeoJson Directory Cleanup");
    }

    public static async Task RemoveGpxFilesNotInCurrentDb(IProgress<string>? progress)
    {
        var db = await Db.Context().ConfigureAwait(false);
        var lineContentIds = await db.LineContents.Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);

        var gpxFiles = UserSettingsSingleton.CurrentSettings().LocalSiteLineGpxDirectory().GetFiles("*.gpx").ToList();

        var fileCount = 0;

        foreach (var loopGpxFiles in gpxFiles)
        {
            fileCount++;

            if (fileCount % 500 == 0)
                progress?.Report(
                    $"ContentData Directory Cleanup - Checking Existing File {fileCount} of {gpxFiles.Count}");

            if (Guid.TryParse(Path.GetFileNameWithoutExtension(loopGpxFiles.Name), out var parsedGuid))
            {
                if (!lineContentIds.Contains(parsedGuid))
                    try
                    {
                        loopGpxFiles.Delete();
                        progress?.Report($"Deleted {loopGpxFiles.FullName}");
                    }
                    catch (Exception e)
                    {
                        Log.ForContext("exception", e.SafeObjectDump()).Debug(
                            "Error Deleting {file} from the Gpx Directory - Error Ignored",
                            loopGpxFiles.FullName);
                    }
            }
            else
            {
                loopGpxFiles.Delete();
            }
        }
    }

    public static async Task RemoveImageDirectoriesNotFoundInCurrentDatabase(IProgress<string>? progress)
    {
        progress?.Report("Starting Directory Cleanup");

        var db = await Db.Context().ConfigureAwait(false);
        var dbFolders = db.ImageContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

        var siteTopLevelImageDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteImageDirectory();

        var firstLevelFolders = siteTopLevelImageDirectory.GetDirectories().OrderBy(x => x.Name).ToList();

        List<DirectoryInfo> folderDirectories = [];

        foreach (var loopFoldersDirectoryInfo in firstLevelFolders)
            if (!dbFolders.Contains(loopFoldersDirectoryInfo.Name))
                loopFoldersDirectoryInfo.Delete(true);
            else
                folderDirectories.Add(loopFoldersDirectoryInfo);

        progress?.Report(
            $"Found {folderDirectories.Count} Existing Image Directories to Check against {dbFolders.Count} Image Folders in the Database");

        foreach (var loopExistingDirectories in folderDirectories)
        {
            if (!dbFolders.Contains(loopExistingDirectories.Name))
            {
                progress?.Report($"Deleting {loopExistingDirectories.FullName}");

                loopExistingDirectories.Delete(true);
                continue;
            }

            progress?.Report($"Staring Image Content Directory Check for {loopExistingDirectories.FullName}");

            var existingContentDirectories = loopExistingDirectories.GetDirectories().OrderBy(x => x.Name).ToList();
            var dbContentSlugs = db.ImageContents.Where(x => x.Folder == loopExistingDirectories.Name)
                .Select(x => x.Slug).OrderBy(x => x).ToList();

            progress?.Report(
                $"Found {existingContentDirectories.Count} Existing Image Content Directories in {loopExistingDirectories.Name} to Check against {dbContentSlugs.Count} Content Items in the Database");

            foreach (var loopExistingContentDirectories in existingContentDirectories)
            {
                if (!dbContentSlugs.Contains(loopExistingContentDirectories.Name))
                {
                    progress?.Report($"Deleting {loopExistingContentDirectories.FullName}");
                    loopExistingContentDirectories.Delete(true);
                    continue;
                }

                var fileList = loopExistingContentDirectories.GetFiles().ToList();
                fileList.RemoveAll(x =>
                    x.Name.StartsWith(loopExistingContentDirectories.Name, StringComparison.OrdinalIgnoreCase));
                fileList.RemoveAll(x => x.Name.StartsWith("Historic", StringComparison.OrdinalIgnoreCase));
                fileList.RemoveAll(ImageGenerator.ImageFileTypeIsSupported);

                if (fileList.Any())
                    progress?.Report(
                        $"Deleting Files in {loopExistingContentDirectories.FullName} - {string.Join(", ", fileList.OrderBy(x => x.Name).Select(x => x.Name))}");

                foreach (var loopFiles in fileList)
                    try
                    {
                        loopFiles.Delete();
                    }
                    catch (Exception e)
                    {
                        Log.ForContext("exception", e.SafeObjectDump())
                            .Debug("Error Deleting {file} - Exception Ignored", loopFiles);
                    }

                progress?.Report($"{loopExistingContentDirectories.FullName} matches current Image Content");
            }
        }

        progress?.Report("Ending Image Directory Cleanup");
    }

    public static async Task RemoveImageMediaArchiveFilesNotInCurrentDatabase(IProgress<string>? progress)
    {
        progress?.Report("Starting Image Media Archive Cleanup");

        var db = await Db.Context().ConfigureAwait(false);
        var siteImageMediaArchiveDirectory =
            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageDirectory();
        var siteImageMediaArchiveFiles = siteImageMediaArchiveDirectory.GetFiles().OrderBy(x => x.Name).ToList();

        var dbNames = db.ImageContents.Select(x => x.OriginalFileName).OrderBy(x => x).ToList();

        progress?.Report(
            $"Found {siteImageMediaArchiveFiles.Count} Existing Image Files in the Media Archive - Checking against {dbNames.Count} Image Names  in the Database");

        foreach (var loopFiles in siteImageMediaArchiveFiles)
        {
            if (!dbNames.Contains(loopFiles.Name))
            {
                progress?.Report($"Deleting {loopFiles.Name}");
                loopFiles.Delete();
                continue;
            }

            progress?.Report($"Found {loopFiles.Name} in Database");
        }
    }

    public static async Task RemoveLineDirectoriesNotFoundInCurrentDatabase(IProgress<string>? progress)
    {
        progress?.Report("Starting Directory Cleanup");

        var db = await Db.Context().ConfigureAwait(false);
        var dbFolders = db.LineContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

        var siteTopLevelLineDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteLineDirectory();

        var firstLevelFolders = siteTopLevelLineDirectory.GetDirectories()
            .Where(x => !x.Name.Equals("GpxData", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name).ToList();

        List<DirectoryInfo> folderDirectories = [];

        foreach (var loopFoldersDirectoryInfo in firstLevelFolders)
            if (!dbFolders.Contains(loopFoldersDirectoryInfo.Name))
                loopFoldersDirectoryInfo.Delete(true);
            else
                folderDirectories.Add(loopFoldersDirectoryInfo);

        progress?.Report(
            $"Found {folderDirectories.Count} Existing Line Directories to Check against {dbFolders.Count} Line Folders in the Database");

        foreach (var loopExistingDirectories in folderDirectories)
        {
            if (!dbFolders.Contains(loopExistingDirectories.Name))
            {
                progress?.Report($"Deleting {loopExistingDirectories.FullName}");

                loopExistingDirectories.Delete(true);
                continue;
            }

            progress?.Report($"Staring Line Content Directory Check for {loopExistingDirectories.FullName}");

            var existingContentDirectories = loopExistingDirectories.GetDirectories().OrderBy(x => x.Name).ToList();
            var dbContentSlugs = db.LineContents.Where(x => x.Folder == loopExistingDirectories.Name)
                .Select(x => x.Slug).OrderBy(x => x).ToList();

            progress?.Report(
                $"Found {existingContentDirectories.Count} Existing Line Content Directories in {loopExistingDirectories.Name} to Check against {dbContentSlugs.Count} Content Items in the Database");

            foreach (var loopExistingContentDirectories in existingContentDirectories)
            {
                if (!dbContentSlugs.Contains(loopExistingContentDirectories.Name))
                {
                    progress?.Report($"Deleting {loopExistingContentDirectories.FullName}");
                    loopExistingContentDirectories.Delete(true);
                    continue;
                }

                var fileList = loopExistingContentDirectories.GetFiles().ToList();
                fileList.RemoveAll(x =>
                    x.Name.StartsWith(loopExistingContentDirectories.Name, StringComparison.OrdinalIgnoreCase));
                fileList.RemoveAll(x => x.Name.StartsWith("Historic", StringComparison.OrdinalIgnoreCase));

                if (fileList.Any())
                    progress?.Report(
                        $"Deleting Files in {loopExistingContentDirectories.FullName} - {string.Join(", ", fileList.OrderBy(x => x.Name).Select(x => x.Name))}");

                foreach (var loopFiles in fileList)
                    try
                    {
                        loopFiles.Delete();
                    }
                    catch (Exception e)
                    {
                        Log.ForContext("exception", e.SafeObjectDump())
                            .Debug("Error Deleting {file} - Exception Ignored", loopFiles);
                    }

                progress?.Report($"{loopExistingContentDirectories.FullName} matches current Line Content");
            }
        }

        progress?.Report("Ending Line Directory Cleanup");
    }

    public static async Task RemoveMapHistoryDataDirectoryFilesNotInCurrentDb(IProgress<string>? progress)
    {
        var db = await Db.Context().ConfigureAwait(false);
        var mapContentIds = await db.MapComponents.Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);

        var mapHistoryDataDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteMapComponentDataDirectory();

        if (!mapHistoryDataDirectory.Exists) return;

        var mapHistoryDataFiles = mapHistoryDataDirectory.GetFiles().ToList();

        foreach (var loopFiles in mapHistoryDataFiles)
        {
            var fileNameParts = Path.GetFileNameWithoutExtension(loopFiles.Name).Split("---");

            if (!loopFiles.Name.StartsWith(UserSettingsUtilities.HistoricMapComponentContentPrefix,
                    StringComparison.OrdinalIgnoreCase) || fileNameParts.Length != 2 ||
                !Guid.TryParse(fileNameParts[1], out var parsedContentId))
            {
                loopFiles.Delete();
                continue;
            }

            if (!mapContentIds.Contains(parsedContentId)) loopFiles.Delete();
        }
    }

    public static async Task RemoveMediaArchiveFilesNotInDatabase(IProgress<string>? progress)
    {
        await RemoveFileMediaArchiveFilesNotInCurrentDatabase(progress).ConfigureAwait(false);
        await RemoveImageMediaArchiveFilesNotInCurrentDatabase(progress).ConfigureAwait(false);
        await RemovePhotoMediaArchiveFilesNotInCurrentDatabase(progress).ConfigureAwait(false);
    }

    public static async Task RemoveNoteDirectoriesNotFoundInCurrentDatabase(IProgress<string>? progress)
    {
        progress?.Report("Starting Directory Cleanup");

        var db = await Db.Context().ConfigureAwait(false);
        var dbFolders = db.NoteContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

        var siteTopLevelNoteDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteNoteDirectory();

        var firstLevelFolders = siteTopLevelNoteDirectory.GetDirectories().OrderBy(x => x.Name).ToList();

        List<DirectoryInfo> folderDirectories = [];

        foreach (var loopFoldersDirectoryInfo in firstLevelFolders)
            if (!dbFolders.Contains(loopFoldersDirectoryInfo.Name))
                loopFoldersDirectoryInfo.Delete(true);
            else
                folderDirectories.Add(loopFoldersDirectoryInfo);

        progress?.Report(
            $"Found {folderDirectories.Count} Existing Note Directories to Check against {dbFolders.Count} Note Folders in the Database");

        foreach (var loopExistingDirectories in folderDirectories)
        {
            if (!dbFolders.Contains(loopExistingDirectories.Name))
            {
                progress?.Report($"Deleting {loopExistingDirectories.FullName}");

                loopExistingDirectories.Delete(true);
                continue;
            }

            progress?.Report($"Staring Note Content Directory Check for {loopExistingDirectories.FullName}");

            var existingFiles = loopExistingDirectories.GetFiles().ToList();
            var dbContentSlugs = db.NoteContents.Where(x =>
                    x.Folder == loopExistingDirectories.Name && !string.IsNullOrWhiteSpace(x.Slug))
                .Select(x => x.Slug!)
                .OrderBy(x => x).ToList();
            var dbContentIds = db.NoteContents.Where(x => x.Folder == loopExistingDirectories.Name)
                .Select(x => x.ContentId.ToString()).ToList();

            progress?.Report(
                $"Found {existingFiles.Count} Existing Note Content and Json Files in {loopExistingDirectories.Name} to Check");

            foreach (var loopExistingFiles in existingFiles)
            {
                var matchesSlug = dbContentSlugs.Any(x => loopExistingFiles.Name.Contains(x));
                var matchesContentId = dbContentIds.Any(x => loopExistingFiles.Name.Contains(x));

                if (matchesSlug || matchesContentId)
                {
                    progress?.Report($"{loopExistingFiles.FullName} matches current Note Content");
                    continue;
                }

                progress?.Report($"Deleting {loopExistingFiles.FullName}");
                loopExistingFiles.Delete();
            }
        }

        progress?.Report("Ending Note Directory Cleanup");
    }

    public static async Task RemovePhotoDirectoriesNotFoundInCurrentDatabase(IProgress<string>? progress)
    {
        progress?.Report("Starting Directory Cleanup");

        var db = await Db.Context().ConfigureAwait(false);
        var dbFolders = db.PhotoContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

        var siteTopLevelPhotoDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePhotoDirectory();

        var firstLevelFolders = siteTopLevelPhotoDirectory.GetDirectories().Where(x => x.Name != "Galleries")
            .OrderBy(x => x.Name).ToList();

        List<DirectoryInfo> folderDirectories = [];

        foreach (var loopFoldersDirectoryInfo in firstLevelFolders)
            if (!dbFolders.Contains(loopFoldersDirectoryInfo.Name))
                loopFoldersDirectoryInfo.Delete(true);
            else
                folderDirectories.Add(loopFoldersDirectoryInfo);

        progress?.Report(
            $"Found {folderDirectories.Count} Existing Photo Directories to Check against {dbFolders.Count} Photo Folders in the Database");

        foreach (var loopExistingDirectories in folderDirectories)
        {
            if (!dbFolders.Contains(loopExistingDirectories.Name))
            {
                progress?.Report($"Deleting {loopExistingDirectories.FullName}");

                loopExistingDirectories.Delete(true);
                continue;
            }

            progress?.Report($"Staring Photo Content Directory Check for {loopExistingDirectories.FullName}");

            var existingContentDirectories = loopExistingDirectories.GetDirectories().OrderBy(x => x.Name).ToList();
            var dbContentSlugs = db.PhotoContents.Where(x => x.Folder == loopExistingDirectories.Name)
                .Select(x => x.Slug).OrderBy(x => x).ToList();

            progress?.Report(
                $"Found {existingContentDirectories.Count} Existing Photo Content Directories in {loopExistingDirectories.Name} to Check against {dbContentSlugs.Count} Content Items in the Database");

            foreach (var loopExistingContentDirectories in existingContentDirectories)
            {
                if (!dbContentSlugs.Contains(loopExistingContentDirectories.Name))
                {
                    progress?.Report($"Deleting {loopExistingContentDirectories.FullName}");
                    loopExistingContentDirectories.Delete(true);
                    continue;
                }

                var fileList = loopExistingContentDirectories.GetFiles().ToList();
                fileList.RemoveAll(x =>
                    x.Name.StartsWith(loopExistingContentDirectories.Name, StringComparison.OrdinalIgnoreCase));
                fileList.RemoveAll(x => x.Name.StartsWith("Historic", StringComparison.OrdinalIgnoreCase));
                fileList.RemoveAll(PhotoGenerator.PhotoFileTypeIsSupported);

                if (fileList.Any())
                    progress?.Report(
                        $"Deleting Files in {loopExistingContentDirectories.FullName} - {string.Join(", ", fileList.OrderBy(x => x.Name).Select(x => x.Name))}");

                foreach (var loopFiles in fileList)
                    try
                    {
                        loopFiles.Delete();
                    }
                    catch (Exception e)
                    {
                        Log.ForContext("exception", e.SafeObjectDump())
                            .Debug("Error Deleting {file} - Exception Ignored", loopFiles);
                    }

                progress?.Report($"{loopExistingContentDirectories.FullName} matches current Photo Content");
            }
        }

        progress?.Report("Ending Content Directory Cleanup");

        //Daily photo purge

        progress?.Report("Starting Daily Photo Content Cleanup");

        var dailyPhotoGalleryDirectory =
            UserSettingsSingleton.CurrentSettings().LocalSiteDailyPhotoGalleryDirectory();

        var dailyGalleryFiles = dailyPhotoGalleryDirectory.GetFiles().OrderBy(x => x.Name).ToList();

        var allPhotoDays = (await db.PhotoContents.Select(x => x.PhotoCreatedOn).Distinct().ToListAsync()
                .ConfigureAwait(false))
            .Select(x => x.Date).Distinct().ToList();

        progress?.Report(
            $"Found {dailyGalleryFiles.Count} Daily Dates in the db, {allPhotoDays.Count} files in daily photo galleries.");

        foreach (var loopGalleryFiles in dailyGalleryFiles)
        {
            var dateTimeForFile =
                UserSettingsUtilities.LocalSiteDailyPhotoGalleryPhotoDateFromFileInfo(loopGalleryFiles);

            if (dateTimeForFile == null || !allPhotoDays.Contains(dateTimeForFile.Value))
            {
                loopGalleryFiles.Delete();
                progress?.Report($"Deleting {loopGalleryFiles.FullName}");
                continue;
            }

            progress?.Report($"{loopGalleryFiles.FullName} matches current content");
        }

        progress?.Report("Ending Daily Photo Content Cleanup");
    }

    public static async Task RemovePhotoMediaArchiveFilesNotInCurrentDatabase(IProgress<string>? progress)
    {
        progress?.Report("Starting Photo Media Archive Cleanup");

        var db = await Db.Context().ConfigureAwait(false);
        var sitePhotoMediaArchiveDirectory =
            UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoDirectory();
        var sitePhotoMediaArchiveFiles = sitePhotoMediaArchiveDirectory.GetFiles().OrderBy(x => x.Name).ToList();

        var dbNames = db.PhotoContents.Select(x => x.OriginalFileName).OrderBy(x => x).ToList();

        progress?.Report(
            $"Found {sitePhotoMediaArchiveFiles.Count} Existing Photo Files in the Media Archive - Checking against {dbNames.Count} Photo Names  in the Database");

        foreach (var loopFiles in sitePhotoMediaArchiveFiles)
        {
            if (!dbNames.Contains(loopFiles.Name))
            {
                progress?.Report($"Deleting {loopFiles.Name}");
                loopFiles.Delete();
                continue;
            }

            progress?.Report($"Found {loopFiles.Name} in Database");
        }
    }

    public static async Task RemovePointDirectoriesNotFoundInCurrentDatabase(IProgress<string>? progress)
    {
        progress?.Report("Starting Directory Cleanup");

        var db = await Db.Context().ConfigureAwait(false);
        var dbFolders = db.PointContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

        var siteTopLevelPointDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePointDirectory();

        var firstLevelFolders = siteTopLevelPointDirectory.GetDirectories().Where(x => x.Name != "Data")
            .OrderBy(x => x.Name).ToList();

        List<DirectoryInfo> folderDirectories = [];

        foreach (var loopFoldersDirectoryInfo in firstLevelFolders)
            if (!dbFolders.Contains(loopFoldersDirectoryInfo.Name))
                loopFoldersDirectoryInfo.Delete(true);
            else
                folderDirectories.Add(loopFoldersDirectoryInfo);

        progress?.Report(
            $"Found {folderDirectories.Count} Existing Point Directories to Check against {dbFolders.Count} Point Folders in the Database");

        foreach (var loopExistingDirectories in folderDirectories)
        {
            if (!dbFolders.Contains(loopExistingDirectories.Name))
            {
                progress?.Report($"Deleting {loopExistingDirectories.FullName}");

                loopExistingDirectories.Delete(true);
                continue;
            }

            progress?.Report($"Staring Point Content Directory Check for {loopExistingDirectories.FullName}");

            var existingContentDirectories = loopExistingDirectories.GetDirectories().OrderBy(x => x.Name).ToList();
            var dbContentSlugs = db.PointContents.Where(x => x.Folder == loopExistingDirectories.Name)
                .Select(x => x.Slug).OrderBy(x => x).ToList();

            progress?.Report(
                $"Found {existingContentDirectories.Count} Existing Point Content Directories in {loopExistingDirectories.Name} to Check against {dbContentSlugs.Count} Content Items in the Database");

            foreach (var loopExistingContentDirectories in existingContentDirectories)
            {
                if (!dbContentSlugs.Contains(loopExistingContentDirectories.Name))
                {
                    progress?.Report($"Deleting {loopExistingContentDirectories.FullName}");
                    loopExistingContentDirectories.Delete(true);
                    continue;
                }

                var fileList = loopExistingContentDirectories.GetFiles().ToList();
                fileList.RemoveAll(x =>
                    x.Name.StartsWith(loopExistingContentDirectories.Name, StringComparison.OrdinalIgnoreCase));
                fileList.RemoveAll(x => x.Name.StartsWith("Historic", StringComparison.OrdinalIgnoreCase));

                if (fileList.Any())
                    progress?.Report(
                        $"Deleting Files in {loopExistingContentDirectories.FullName} - {string.Join(", ", fileList.OrderBy(x => x.Name).Select(x => x.Name))}");

                foreach (var loopFiles in fileList)
                    try
                    {
                        loopFiles.Delete();
                    }
                    catch (Exception e)
                    {
                        Log.ForContext("exception", e.SafeObjectDump())
                            .Debug("Error Deleting {file} - Exception Ignored", loopFiles);
                    }

                progress?.Report($"{loopExistingContentDirectories.FullName} matches current Point Content");
            }
        }

        progress?.Report("Ending Point Directory Cleanup");
    }

    public static async Task RemovePostDirectoriesNotFoundInCurrentDatabase(IProgress<string>? progress)
    {
        progress?.Report("Starting Directory Cleanup");

        var db = await Db.Context().ConfigureAwait(false);
        var dbFolders = db.PostContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

        var siteTopLevelPostDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePostDirectory();

        var firstLevelFolders = siteTopLevelPostDirectory.GetDirectories().OrderBy(x => x.Name).ToList();

        List<DirectoryInfo> folderDirectories = [];

        foreach (var loopFoldersDirectoryInfo in firstLevelFolders)
            if (!dbFolders.Contains(loopFoldersDirectoryInfo.Name))
                loopFoldersDirectoryInfo.Delete(true);
            else
                folderDirectories.Add(loopFoldersDirectoryInfo);

        progress?.Report(
            $"Found {folderDirectories.Count} Existing Post Directories to Check against {dbFolders.Count} Post Folders in the Database");

        foreach (var loopExistingDirectories in folderDirectories)
        {
            if (!dbFolders.Contains(loopExistingDirectories.Name))
            {
                progress?.Report($"Deleting {loopExistingDirectories.FullName}");

                loopExistingDirectories.Delete(true);
                continue;
            }

            progress?.Report($"Staring Post Content Directory Check for {loopExistingDirectories.FullName}");

            var existingContentDirectories = loopExistingDirectories.GetDirectories().OrderBy(x => x.Name).ToList();
            var dbContentSlugs = db.PostContents.Where(x => x.Folder == loopExistingDirectories.Name)
                .Select(x => x.Slug).OrderBy(x => x).ToList();

            progress?.Report(
                $"Found {existingContentDirectories.Count} Existing Post Content Directories in {loopExistingDirectories.Name} to Check against {dbContentSlugs.Count} Content Items in the Database");

            foreach (var loopExistingContentDirectories in existingContentDirectories)
            {
                if (!dbContentSlugs.Contains(loopExistingContentDirectories.Name))
                {
                    progress?.Report($"Deleting {loopExistingContentDirectories.FullName}");
                    loopExistingContentDirectories.Delete(true);
                    continue;
                }

                var fileList = loopExistingContentDirectories.GetFiles().ToList();
                fileList.RemoveAll(x =>
                    x.Name.StartsWith(loopExistingContentDirectories.Name, StringComparison.OrdinalIgnoreCase));
                fileList.RemoveAll(x => x.Name.StartsWith("Historic", StringComparison.OrdinalIgnoreCase));

                if (fileList.Any())
                    progress?.Report(
                        $"Deleting Files in {loopExistingContentDirectories.FullName} - {string.Join(", ", fileList.OrderBy(x => x.Name).Select(x => x.Name))}");

                foreach (var loopFiles in fileList)
                    try
                    {
                        loopFiles.Delete();
                    }
                    catch (Exception e)
                    {
                        Log.ForContext("exception", e.SafeObjectDump())
                            .Debug("Error Deleting {file} - Exception Ignored", loopFiles);
                    }

                progress?.Report($"{loopExistingContentDirectories.FullName} matches current Post Content");
            }
        }

        progress?.Report("Ending Post Directory Cleanup");
    }

    public static async Task RemoveTagContentFilesNotInCurrentDatabase(IProgress<string>? progress)
    {
        progress?.Report("Starting Tag Directory Cleanup");

        var tags = (await Db.TagSlugsAndContentList(true, false, progress).ConfigureAwait(false)).Select(x => x.tag)
            .Distinct().ToList();

        var tagFiles = UserSettingsSingleton.CurrentSettings().LocalSiteTagsDirectory().GetFiles("TagList-*.html")
            .OrderBy(x => x.Name).ToList();

        foreach (var loopFiles in tagFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(loopFiles.Name);
            var fileTag = fileName[8..];

            if (!tags.Contains(fileTag)) loopFiles.Delete();
        }
    }

    public static async Task RemoveVideoDirectoriesNotFoundInCurrentDatabase(IProgress<string>? progress)
    {
        progress?.Report("Starting Directory Cleanup");

        var db = await Db.Context().ConfigureAwait(false);
        var dbFolders = db.VideoContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

        var siteTopLevelVideoDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteVideoDirectory();

        var firstLevelFolders = siteTopLevelVideoDirectory.GetDirectories().OrderBy(x => x.Name).ToList();

        List<DirectoryInfo> folderDirectories = [];

        foreach (var loopFoldersDirectoryInfo in firstLevelFolders)
            if (!dbFolders.Contains(loopFoldersDirectoryInfo.Name))
                loopFoldersDirectoryInfo.Delete(true);
            else
                folderDirectories.Add(loopFoldersDirectoryInfo);

        progress?.Report(
            $"Found {folderDirectories.Count} Existing Video Directories to Check against {dbFolders.Count} Video Folders in the Database");

        foreach (var loopExistingDirectories in folderDirectories)
        {
            if (!dbFolders.Contains(loopExistingDirectories.Name))
            {
                progress?.Report($"Deleting {loopExistingDirectories.FullName}");

                loopExistingDirectories.Delete(true);
                continue;
            }

            progress?.Report($"Staring Video Content Directory Check for {loopExistingDirectories.FullName}");

            var existingContentDirectories = loopExistingDirectories.GetDirectories().OrderBy(x => x.Name).ToList();
            var dbContentSlugs = db.VideoContents.Where(x => x.Folder == loopExistingDirectories.Name)
                .Select(x => x.Slug).OrderBy(x => x).ToList();

            progress?.Report(
                $"Found {existingContentDirectories.Count} Existing Video Content Directories in {loopExistingDirectories.Name} to Check against {dbContentSlugs.Count} Content Items in the Database");

            foreach (var loopExistingContentDirectories in existingContentDirectories)
            {
                if (!dbContentSlugs.Contains(loopExistingContentDirectories.Name))
                {
                    progress?.Report($"Deleting {loopExistingContentDirectories.FullName}");
                    loopExistingContentDirectories.Delete(true);
                    continue;
                }

                var fileList = loopExistingContentDirectories.GetFiles().ToList();
                fileList.RemoveAll(x =>
                    x.Name.StartsWith(loopExistingContentDirectories.Name, StringComparison.OrdinalIgnoreCase));
                fileList.RemoveAll(x => x.Name.StartsWith("Historic", StringComparison.OrdinalIgnoreCase));
                fileList.RemoveAll(VideoGenerator.VideoFileTypeIsSupported);

                if (fileList.Any())
                    progress?.Report(
                        $"Deleting Files in {loopExistingContentDirectories.FullName} - {string.Join(", ", fileList.OrderBy(x => x.Name).Select(x => x.Name))}");

                foreach (var loopFiles in fileList)
                    try
                    {
                        loopFiles.Delete();
                    }
                    catch (Exception e)
                    {
                        Log.ForContext("exception", e.SafeObjectDump())
                            .Debug("Error Deleting {file} - Exception Ignored", loopFiles);
                    }

                progress?.Report($"{loopExistingContentDirectories.FullName} matches current Video Content");
            }
        }

        progress?.Report("Ending Video Directory Cleanup");
    }

    public static async Task<string> SpatialScriptsAsString()
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        var siteResources = embeddedProvider.GetDirectoryContents("")
            .Single(x => x.Name.Contains("pointless-waymarks-spatial-common"));

        await using var stream = siteResources.CreateReadStream();
        using StreamReader reader = new(stream);
        var spatialScript = await reader.ReadToEndAsync().ConfigureAwait(false);

        return spatialScript;
    }

    /// <summary>
    ///     Verify or Create all top level folders for a site - includes both local only directories like the Media Archive and
    ///     the top level folders for the generated site.
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    public static List<GenerationReturn> VerifyOrCreateAllTopLevelFolders(this UserSettings settings)
    {
        var mediaFolders = settings.VerifyOrCreateMediaArchiveFolders();
        var topLevelSite = settings.VerifyOrCreateLocalTopLevelSiteFolders();
        return mediaFolders.Concat(topLevelSite).ToList();
    }

    /// <summary>
    ///     Verify or Create top level folders needed for the Generated Site
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    public static List<GenerationReturn> VerifyOrCreateLocalTopLevelSiteFolders(this UserSettings settings)
    {
        return
        [
            settings.LocalSiteDirectory().CreateIfItDoesNotExist(),
            settings.LocalSiteFileDirectory().CreateIfItDoesNotExist(),
            settings.LocalSiteGeoJsonDirectory().CreateIfItDoesNotExist(),
            settings.LocalSiteImageDirectory().CreateIfItDoesNotExist(),
            settings.LocalSiteLineDirectory().CreateIfItDoesNotExist(),
            settings.LocalSiteLinkDirectory().CreateIfItDoesNotExist(),
            settings.LocalSiteMapComponentDirectory().CreateIfItDoesNotExist(),
            settings.LocalSiteMapComponentDataDirectory().CreateIfItDoesNotExist(),
            settings.LocalSiteNoteDirectory().CreateIfItDoesNotExist(),
            settings.LocalSitePhotoDirectory().CreateIfItDoesNotExist(),
            settings.LocalSitePhotoGalleryDirectory().CreateIfItDoesNotExist(),
            settings.LocalSiteDailyPhotoGalleryDirectory().CreateIfItDoesNotExist(),
            settings.LocalSitePointDirectory().CreateIfItDoesNotExist(),
            settings.LocalSitePointDataDirectory().CreateIfItDoesNotExist(),
            settings.LocalSitePostDirectory().CreateIfItDoesNotExist(),
            settings.LocalSiteVideoDirectory().CreateIfItDoesNotExist(),
            settings.LocalSiteTagsDirectory().CreateIfItDoesNotExist(),
            settings.LocalSiteSiteResourcesDirectory().CreateIfItDoesNotExist(),
            settings.LocalSiteContentDataDirectory().CreateIfItDoesNotExist()
        ];
    }

    /// <summary>
    ///     Verify or Create the Media Archive Folders.
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    public static List<GenerationReturn> VerifyOrCreateMediaArchiveFolders(this UserSettings settings)
    {
        return
        [
            settings.LocalMediaArchiveFullDirectory().CreateIfItDoesNotExist(),
            settings.LocalMediaArchiveFileDirectory().CreateIfItDoesNotExist(),
            settings.LocalMediaArchiveImageDirectory().CreateIfItDoesNotExist(),
            settings.LocalMediaArchivePhotoDirectory().CreateIfItDoesNotExist(),
            settings.LocalMediaArchiveVideoDirectory().CreateIfItDoesNotExist(),
            settings.LocalMediaArchiveLogsDirectory().CreateIfItDoesNotExist()
        ];
    }

    public static async Task WriteAllTextToFileAndLog(string path, string contents)
    {
        await File.WriteAllTextAsync(path, contents).ConfigureAwait(false);

        await LogFileWriteAsync(path).ConfigureAwait(false);
    }

    public static async Task WriteAllTextToFileAndLog(string path, string contents, Encoding encoding)
    {
        await File.WriteAllTextAsync(path, contents, encoding).ConfigureAwait(false);

        await LogFileWriteAsync(path).ConfigureAwait(false);
    }

    public static async Task WriteAllTextToFileAndLogAsync(string path, string contents)
    {
        await File.WriteAllTextAsync(path, contents).ConfigureAwait(false);

        await LogFileWriteAsync(path).ConfigureAwait(false);
    }

    public static async Task WriteAllTextToFileAndLogAsync(string path, string contents, Encoding encoding)
    {
        await File.WriteAllTextAsync(path, contents, encoding).ConfigureAwait(false);

        await LogFileWriteAsync(path).ConfigureAwait(false);
    }

    public static async Task WriteChangedSiteResourcesToGeneratedSite(IProgress<string>? progress = null)
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        var siteResources = embeddedProvider.GetDirectoryContents("");

        foreach (var loopSiteResources in siteResources.Where(x => x.Name.StartsWith("SiteResources")))
        {
            var fileAsStream = loopSiteResources.CreateReadStream();

            string filePathStyleName;

            if (loopSiteResources.Name.StartsWith("SiteResources.images."))
                filePathStyleName = $"SiteResources\\images\\{loopSiteResources.Name[21..]}";
            else if (loopSiteResources.Name.StartsWith("SiteResources."))
                filePathStyleName = $"SiteResources\\{loopSiteResources.Name[14..]}";
            else
                filePathStyleName = loopSiteResources.Name;

            var destinationFile =
                new FileInfo(Path.Combine(UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName,
                    filePathStyleName));

            if (destinationFile.Exists)
            {
                var streamMd5 = FileAndFolderTools.CalculateMD5(fileAsStream);
                var diskMd5 = FileAndFolderTools.CalculateMD5(destinationFile.FullName);

                if (streamMd5 == diskMd5)
                {
                    progress?.Report(
                        $"Site Resources - Skipping {loopSiteResources.Name} to {destinationFile.FullName} - No Change");
                    continue;
                }
            }

            var destinationDirectory = destinationFile.Directory;
            if (destinationDirectory is { Exists: false }) destinationDirectory.Create();

            var fileStream = File.Create(destinationFile.FullName);
            fileAsStream.Seek(0, SeekOrigin.Begin);
            await fileAsStream.CopyToAsync(fileStream).ConfigureAwait(false);
            fileStream.Close();

            await LogFileWriteAsync(destinationFile.FullName).ConfigureAwait(false);

            progress?.Report($"Site Resources - Writing {loopSiteResources.Name} to {destinationFile.FullName}");
        }
    }

    public static async Task WriteFavIconToGeneratedSite(IProgress<string>? progress)
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        var siteResources = embeddedProvider.GetDirectoryContents("").Single(x => x.Name == "favicon.ico");

        var fileAsStream = siteResources.CreateReadStream();

        var destinationFile =
            new FileInfo(Path.Combine(UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName,
                siteResources.Name));

        var destinationDirectory = destinationFile.Directory;
        if (destinationDirectory is { Exists: false }) destinationDirectory.Create();

        var fileStream = File.Create(destinationFile.FullName);
        fileAsStream.Seek(0, SeekOrigin.Begin);
        await fileAsStream.CopyToAsync(fileStream).ConfigureAwait(false);
        fileStream.Close();

        await LogFileWriteAsync(destinationFile.FullName).ConfigureAwait(false);

        progress?.Report($"Site Resources - Writing {siteResources.Name} to {destinationFile.FullName}");
    }

    public static async Task WriteSelectedFileContentFileToMediaArchive(FileInfo selectedFile)
    {
        var userSettings = UserSettingsSingleton.CurrentSettings();
        var destinationFileName = Path.Combine(userSettings.LocalMediaArchiveFileDirectory().FullName,
            selectedFile.Name);

        if (destinationFileName == selectedFile.FullName) return;

        var destinationFile = new FileInfo(destinationFileName);

        if (destinationFile.Exists) destinationFile.Delete();

        await selectedFile.CopyToAndLog(destinationFileName).ConfigureAwait(false);
    }

    public static async Task<GenerationReturn> WriteSelectedFileContentFileToMediaArchive(FileInfo selectedFile,
        bool replaceExisting)
    {
        var userSettings = UserSettingsSingleton.CurrentSettings();

        var destinationFileName = Path.Combine(userSettings.LocalMediaArchiveFileDirectory().FullName,
            selectedFile.Name);
        if (destinationFileName == selectedFile.FullName && !replaceExisting)
            return GenerationReturn.Success("File is already in Media Archive");

        var destinationFile = new FileInfo(destinationFileName);

        if (destinationFile.Exists) destinationFile.Delete();

        await selectedFile.CopyToAndLogAsync(destinationFileName).ConfigureAwait(false);

        return GenerationReturn.Success("File is copied to Media Archive");
    }

    public static async Task WriteSelectedImageContentFileToMediaArchive(FileInfo selectedFile)
    {
        var userSettings = UserSettingsSingleton.CurrentSettings();
        var destinationFileName = Path.Combine(userSettings.LocalMediaArchiveImageDirectory().FullName,
            selectedFile.Name);

        if (destinationFileName == selectedFile.FullName) return;

        var destinationFile = new FileInfo(destinationFileName);

        if (destinationFile.Exists) destinationFile.Delete();

        await selectedFile.CopyToAndLog(destinationFileName).ConfigureAwait(false);
    }

    public static async Task<GenerationReturn> WriteSelectedImageContentFileToMediaArchive(FileInfo selectedFile,
        bool replaceExisting)
    {
        var userSettings = UserSettingsSingleton.CurrentSettings();

        var destinationFileName = Path.Combine(userSettings.LocalMediaArchiveImageDirectory().FullName,
            selectedFile.Name);
        if (destinationFileName == selectedFile.FullName && !replaceExisting)
            return GenerationReturn.Success("Image is already in Media Archive");

        var destinationFile = new FileInfo(destinationFileName);

        if (destinationFile.Exists) destinationFile.Delete();

        await selectedFile.CopyToAndLogAsync(destinationFileName).ConfigureAwait(false);

        return GenerationReturn.Success("Image is copied to Media Archive");
    }

    public static async Task WriteSelectedPhotoContentFileToMediaArchive(FileInfo selectedFile)
    {
        var userSettings = UserSettingsSingleton.CurrentSettings();
        var destinationFileName = Path.Combine(userSettings.LocalMediaArchivePhotoDirectory().FullName,
            selectedFile.Name);

        if (destinationFileName == selectedFile.FullName) return;

        var destinationFile = new FileInfo(destinationFileName);

        if (destinationFile.Exists) destinationFile.Delete();

        await selectedFile.CopyToAndLog(destinationFileName).ConfigureAwait(false);
    }

    public static async Task<GenerationReturn> WriteSelectedPhotoContentFileToMediaArchive(FileInfo selectedFile,
        bool replaceExisting)
    {
        var userSettings = UserSettingsSingleton.CurrentSettings();

        var destinationFileName = Path.Combine(userSettings.LocalMediaArchivePhotoDirectory().FullName,
            selectedFile.Name);
        if (destinationFileName == selectedFile.FullName && !replaceExisting)
            return GenerationReturn.Success("Photo is already in Media Archive");

        var destinationFile = new FileInfo(destinationFileName);

        if (destinationFile.Exists) destinationFile.Delete();

        await selectedFile.CopyToAndLogAsync(destinationFileName).ConfigureAwait(false);

        return GenerationReturn.Success("Photo is copied to Media Archive");
    }

    public static async Task WriteSelectedVideoContentFileToMediaArchive(FileInfo selectedFile)
    {
        var userSettings = UserSettingsSingleton.CurrentSettings();
        var destinationFileName = Path.Combine(userSettings.LocalMediaArchiveVideoDirectory().FullName,
            selectedFile.Name);

        if (destinationFileName == selectedFile.FullName) return;

        var destinationFile = new FileInfo(destinationFileName);

        if (destinationFile.Exists) destinationFile.Delete();

        await selectedFile.CopyToAndLog(destinationFileName).ConfigureAwait(false);
    }

    public static async Task<GenerationReturn> WriteSelectedVideoContentFileToMediaArchive(FileInfo selectedFile,
        bool replaceExisting)
    {
        var userSettings = UserSettingsSingleton.CurrentSettings();

        var destinationFileName = Path.Combine(userSettings.LocalMediaArchiveVideoDirectory().FullName,
            selectedFile.Name);
        if (destinationFileName == selectedFile.FullName && !replaceExisting)
            return GenerationReturn.Success("File is already in Media Archive");

        var destinationFile = new FileInfo(destinationFileName);

        if (destinationFile.Exists) destinationFile.Delete();

        await selectedFile.CopyToAndLogAsync(destinationFileName).ConfigureAwait(false);

        return GenerationReturn.Success("File is copied to Media Archive");
    }

    public static async Task WriteSiteResourcesToGeneratedSite(IProgress<string>? progress = null)
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        var siteResources = embeddedProvider.GetDirectoryContents("");

        foreach (var loopSiteResources in siteResources.Where(x => x.Name.StartsWith("SiteResources")))
        {
            var fileAsStream = loopSiteResources.CreateReadStream();

            string filePathStyleName;

            if (loopSiteResources.Name.StartsWith("SiteResources.images."))
                filePathStyleName = $"SiteResources\\images\\{loopSiteResources.Name[21..]}";
            else if (loopSiteResources.Name.StartsWith("SiteResources."))
                filePathStyleName = $"SiteResources\\{loopSiteResources.Name[14..]}";
            else
                filePathStyleName = loopSiteResources.Name;

            var destinationFile =
                new FileInfo(Path.Combine(UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName,
                    filePathStyleName));

            var destinationDirectory = destinationFile.Directory;
            if (destinationDirectory is { Exists: false }) destinationDirectory.Create();

            var fileStream = File.Create(destinationFile.FullName);
            fileAsStream.Seek(0, SeekOrigin.Begin);
            await fileAsStream.CopyToAsync(fileStream).ConfigureAwait(false);
            fileStream.Close();

            await LogFileWriteAsync(destinationFile.FullName).ConfigureAwait(false);

            progress?.Report($"Site Resources - Writing {loopSiteResources.Name} to {destinationFile.FullName}");
        }
    }

    public static async Task WriteStylesCssToGeneratedSite(IProgress<string>? progress)
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        var siteResources = embeddedProvider.GetDirectoryContents("").Single(x => x.Name == "style.css");

        var fileAsStream = siteResources.CreateReadStream();

        var destinationFile =
            new FileInfo(Path.Combine(UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName,
                siteResources.Name));

        var destinationDirectory = destinationFile.Directory;
        if (destinationDirectory is { Exists: false }) destinationDirectory.Create();

        var fileStream = File.Create(destinationFile.FullName);
        fileAsStream.Seek(0, SeekOrigin.Begin);
        await fileAsStream.CopyToAsync(fileStream).ConfigureAwait(false);
        fileStream.Close();

        await LogFileWriteAsync(destinationFile.FullName).ConfigureAwait(false);

        progress?.Report($"Site Resources - Writing {siteResources.Name} to {destinationFile.FullName}");
    }
}