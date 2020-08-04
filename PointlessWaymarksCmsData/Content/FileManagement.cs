using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Content
{
    public static class FileManagement
    {
        public static async Task<List<GenerationReturn>> CheckContentFolderStructure(this UserSettings settings)
        {
            var db = await Db.Context();

            var returnList = new List<GenerationReturn>();

            var y = await Task.WhenAll((await db.ImageContents.ToListAsync()).Select(x =>
                GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSiteImageContentDirectory(x),
                    $"Check Content Folder for Image {x.Title}")));

            returnList.AddRange(await Task.WhenAll((await db.ImageContents.ToListAsync()).Select(x =>
                GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSiteImageContentDirectory(x),
                    $"Check Content Folder for Image {x.Title}"))));

            returnList.AddRange(await Task.WhenAll((await db.FileContents.ToListAsync()).Select(x =>
                GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSiteFileContentDirectory(x),
                    $"Check Content Folder for File {x.Title}"))));

            returnList.AddRange(await Task.WhenAll((await db.NoteContents.ToListAsync()).Select(x =>
                GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSiteNoteContentDirectory(x),
                    $"Check Content Folder for Note {x.Title}"))));

            returnList.AddRange(await Task.WhenAll((await db.PostContents.ToListAsync()).Select(x =>
                GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSitePostContentDirectory(x),
                    $"Check Content Folder for Post {x.Title}"))));

            returnList.AddRange(await Task.WhenAll((await db.PhotoContents.ToListAsync()).Select(x =>
                GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSitePhotoContentDirectory(x),
                    $"Check Content Folder for Photo {x.Title}"))));

            return returnList;
        }

        public static async Task<GenerationReturn> CheckFileOriginalFileIsInMediaAndContentDirectories(
            FileContent dbContent, IProgress<string> progress)
        {
            if (dbContent == null)
                return await GenerationReturn.Error(
                    "Null File Content was submitted to the Check of File in the Media and Content Directories");

            UserSettingsSingleton.CurrentSettings().VerifyOrCreateAllTopLevelFolders(progress);

            if (string.IsNullOrWhiteSpace(dbContent.OriginalFileName))
                return await GenerationReturn.Error($"File {dbContent.Title} does not have an Original File assigned",
                    dbContent.ContentId);

            var archiveFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileDirectory().FullName,
                dbContent.OriginalFileName));

            var fileContentDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(dbContent);

            var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, dbContent.OriginalFileName));

            if (!archiveFile.Exists && !contentFile.Exists)
                return await GenerationReturn.Error(
                    $"Neither {archiveFile.FullName} nor {contentFile.FullName} exists - " +
                    $"there appears to be a file missing for File Title {dbContent.Title} " + $"slug {dbContent.Slug}",
                    dbContent.ContentId);


            if (archiveFile.Exists && !contentFile.Exists) archiveFile.CopyTo(contentFile.FullName);

            if (!archiveFile.Exists && contentFile.Exists) contentFile.CopyTo(archiveFile.FullName);

            archiveFile.Refresh();
            contentFile.Refresh();

            var bothFilesPresent = archiveFile.Exists && contentFile.Exists;

            if (bothFilesPresent)
                return await GenerationReturn.Success(
                    $"File {dbContent.Title} Present in both Content and Media Folders", dbContent.ContentId);

            return await GenerationReturn.Error(
                $"There was a problem - Archive File Present: {archiveFile.Exists}, " +
                $"Content File Present {contentFile.Exists} - {archiveFile.FullName}; {contentFile.FullName}",
                dbContent.ContentId);
        }

        public static async Task<GenerationReturn> CheckImageFileIsInMediaAndContentDirectories(ImageContent dbContent,
            IProgress<string> progress)
        {
            if (dbContent == null)
                return await GenerationReturn.Error(
                    "Null Image Content was submitted to the Check of File in the Media and Content Directories");

            UserSettingsSingleton.CurrentSettings().VerifyOrCreateAllTopLevelFolders(progress);

            if (string.IsNullOrWhiteSpace(dbContent.OriginalFileName))
                return await GenerationReturn.Error($"Image {dbContent.Title} does not have an Original File assigned",
                    dbContent.ContentId);

            var archiveFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageDirectory().FullName,
                dbContent.OriginalFileName));

            var fileContentDirectory =
                UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(dbContent);

            var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, dbContent.OriginalFileName));

            if (!archiveFile.Exists && !contentFile.Exists)
                return await GenerationReturn.Error(
                    $"Neither {archiveFile.FullName} nor {contentFile.FullName} exists - " +
                    $"there appears to be a file missing for Image Title {dbContent.Title} " + $"slug {dbContent.Slug}",
                    dbContent.ContentId);


            if (archiveFile.Exists && !contentFile.Exists) archiveFile.CopyTo(contentFile.FullName);

            if (!archiveFile.Exists && contentFile.Exists) contentFile.CopyTo(archiveFile.FullName);

            archiveFile.Refresh();
            contentFile.Refresh();

            var bothFilesPresent = archiveFile.Exists && contentFile.Exists;

            if (bothFilesPresent)
                return await GenerationReturn.Success(
                    $"Image {dbContent.Title} Present in both Content and Media Folders", dbContent.ContentId);

            return await GenerationReturn.Error(
                $"There was a problem - Archive Image Present: {archiveFile.Exists}, " +
                $"Content Image Present {contentFile.Exists} - {archiveFile.FullName}; {contentFile.FullName}",
                dbContent.ContentId);
        }

        public static async Task<GenerationReturn> CheckPhotoFileIsInMediaAndContentDirectories(PhotoContent dbContent,
            IProgress<string> progress)
        {
            if (dbContent == null)
                return await GenerationReturn.Error(
                    "Null Photo Content was submitted to the Check of File in the Media and Content Directories");

            UserSettingsSingleton.CurrentSettings().VerifyOrCreateAllTopLevelFolders(progress);

            if (string.IsNullOrWhiteSpace(dbContent.OriginalFileName))
                return await GenerationReturn.Error($"Photo {dbContent.Title} does not have an Original File assigned",
                    dbContent.ContentId);

            var archiveFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoDirectory().FullName,
                dbContent.OriginalFileName));

            var fileContentDirectory =
                UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(dbContent);

            var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, dbContent.OriginalFileName));

            if (!archiveFile.Exists && !contentFile.Exists)
                return await GenerationReturn.Error(
                    $"Neither {archiveFile.FullName} nor {contentFile.FullName} exists - " +
                    $"there appears to be a file missing for Photo Title {dbContent.Title} " + $"slug {dbContent.Slug}",
                    dbContent.ContentId);

            if (archiveFile.Exists && !contentFile.Exists) archiveFile.CopyTo(contentFile.FullName);

            if (!archiveFile.Exists && contentFile.Exists) contentFile.CopyTo(archiveFile.FullName);

            archiveFile.Refresh();
            contentFile.Refresh();

            var bothFilesPresent = archiveFile.Exists && contentFile.Exists;

            if (bothFilesPresent)
                return await GenerationReturn.Success(
                    $"Photo {dbContent.Title} Present in both Content and Media Folders", dbContent.ContentId);

            return await GenerationReturn.Error(
                $"There was a problem - Archive Photo Present: {archiveFile.Exists}, " +
                $"Content Photo Present {contentFile.Exists} - {archiveFile.FullName}; {contentFile.FullName}",
                dbContent.ContentId);
        }

        public static async Task<List<GenerationReturn>> CleanAndResizeAllImageFiles(IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.ImageContents.ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress.Report($"Found {totalCount} Images to Clean and Resize");

            var returnList = new List<GenerationReturn>();

            foreach (var loopItem in allItems)
            {
                progress.Report($"Image Clean and Resize for {loopItem.Title} - {loopCount} of {totalCount}");

                returnList.Add(await PictureResizing.CopyCleanResizeImage(loopItem, progress));

                loopCount++;
            }

            return returnList;
        }

        public static async Task<List<GenerationReturn>> CleanAndResizeAllPhotoFiles(IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.PhotoContents.ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress.Report($"Found {totalCount} Photos to Clean and Resize");

            var returnList = new List<GenerationReturn>();

            foreach (var loopItem in allItems)
            {
                progress.Report($"Photo Clean and Resize for {loopItem.Title} - {loopCount} of {totalCount}");

                returnList.Add(await PictureResizing.CopyCleanResizePhoto(loopItem, progress));

                loopCount++;
            }

            return returnList;
        }

        public static async Task CleanUpTemporaryFiles()
        {
            var temporaryDirectory = UserSettingsUtilities.TempStorageDirectory();

            if (!temporaryDirectory.Exists)
            {
                temporaryDirectory.Create();
                return;
            }

            var allFiles = temporaryDirectory.GetFiles().ToList();

            var frozenUtcNow = DateTime.UtcNow;

            foreach (var loopFiles in allFiles)
                try
                {
                    var creationDayDiff = frozenUtcNow.Subtract(loopFiles.CreationTimeUtc).Days;
                    var lastAccessDayDiff = frozenUtcNow.Subtract(loopFiles.LastAccessTimeUtc).Days;
                    var lastWriteDayDiff = frozenUtcNow.Subtract(loopFiles.LastWriteTimeUtc).Days;

                    if (creationDayDiff > 28 && lastAccessDayDiff > 28 && lastWriteDayDiff > 28)
                        loopFiles.Delete();
                }
                catch (Exception e)
                {
                    await EventLogContext.TryWriteExceptionToLog(e, "FileManagement.CleanUpTemporaryFiles",
                        $"Could not delete temporary file - {e}");
                }
        }

        public static async Task<List<GenerationReturn>> ConfirmAllFileContentFilesArePresent(
            IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.FileContents.Where(x => string.IsNullOrEmpty(x.OriginalFileName)).ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress.Report($"Found {totalCount} Files to Check");

            var returnList = new List<GenerationReturn>();

            foreach (var loopItem in allItems)
            {
                progress.Report($"File Check for {loopItem.Title} - {loopCount} of {totalCount}");

                returnList.Add(await CheckFileOriginalFileIsInMediaAndContentDirectories(loopItem, progress));

                loopCount++;
            }

            return returnList;
        }

        public static async Task RemoveContentDirectoriesAndFilesNotFoundInCurrentDatabase(IProgress<string> progress)
        {
            await RemoveFileDirectoriesNotFoundInCurrentDatabase(progress);
            await RemoveImageDirectoriesNotFoundInCurrentDatabase(progress);
            await RemoveNoteDirectoriesNotFoundInCurrentDatabase(progress);
            await RemovePhotoDirectoriesNotFoundInCurrentDatabase(progress);
            await RemovePhotoDirectoriesNotFoundInCurrentDatabase(progress);
        }

        public static async Task RemoveFileDirectoriesNotFoundInCurrentDatabase(IProgress<string> progress)
        {
            progress?.Report("Starting Directory Cleanup");

            var db = await Db.Context();
            var dbFolders = db.FileContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

            var siteTopLevelFileDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteFileDirectory();
            var folderDirectories = siteTopLevelFileDirectory.GetDirectories().OrderBy(x => x.Name).ToList();

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

            progress?.Report("Ending File Directory Cleanup");
        }

        public static async Task RemoveFileMediaArchiveFilesNotInCurrentDatabase(IProgress<string> progress)
        {
            progress?.Report("Starting File Media Archive Cleanup");

            var db = await Db.Context();
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

        public static async Task RemoveImageDirectoriesNotFoundInCurrentDatabase(IProgress<string> progress)
        {
            progress?.Report("Starting Directory Cleanup");

            var db = await Db.Context();
            var dbFolders = db.ImageContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

            var siteTopLevelImageDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteImageDirectory();
            var folderDirectories = siteTopLevelImageDirectory.GetDirectories().OrderBy(x => x.Name).ToList();

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

                    progress?.Report($"{loopExistingContentDirectories.FullName} matches current Image Content");
                }
            }

            progress?.Report("Ending Image Directory Cleanup");
        }

        public static async Task RemoveImageMediaArchiveFilesNotInCurrentDatabase(IProgress<string> progress)
        {
            progress?.Report("Starting Image Media Archive Cleanup");

            var db = await Db.Context();
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

        public static async Task RemoveMediaArchiveFilesNotInDatabase(IProgress<string> progress)
        {
            await RemoveFileMediaArchiveFilesNotInCurrentDatabase(progress);
            await RemoveImageMediaArchiveFilesNotInCurrentDatabase(progress);
            await RemovePhotoMediaArchiveFilesNotInCurrentDatabase(progress);
        }

        public static async Task RemoveNoteDirectoriesNotFoundInCurrentDatabase(IProgress<string> progress)
        {
            progress?.Report("Starting Directory Cleanup");

            var db = await Db.Context();
            var dbFolders = db.NoteContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

            var siteTopLevelNoteDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteNoteDirectory();
            var folderDirectories = siteTopLevelNoteDirectory.GetDirectories().OrderBy(x => x.Name).ToList();

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
                var dbContentSlugs = db.NoteContents.Where(x => x.Folder == loopExistingDirectories.Name)
                    .Select(x => x.Slug).OrderBy(x => x).ToList();
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

        public static async Task RemovePhotoDirectoriesNotFoundInCurrentDatabase(IProgress<string> progress)
        {
            progress?.Report("Starting Directory Cleanup");

            var db = await Db.Context();
            var dbPhotoFolders = db.PhotoContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

            var siteTopLevelPhotoDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePhotoDirectory();
            var photoFolderDirectories = siteTopLevelPhotoDirectory.GetDirectories().Where(x => x.Name != "Galleries")
                .OrderBy(x => x.Name).ToList();

            progress?.Report(
                $"Found {photoFolderDirectories.Count} Existing Photo Directories to Check against {dbPhotoFolders.Count} Photo Folders in the Database");

            foreach (var loopExistingDirectories in photoFolderDirectories)
            {
                if (!dbPhotoFolders.Contains(loopExistingDirectories.Name))
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

                    progress?.Report($"{loopExistingContentDirectories.FullName} matches current Photo Content");
                }
            }

            progress?.Report("Ending Content Directory Cleanup");

            //Daily photo purge

            progress?.Report("Starting Daily Photo Content Cleanup");

            var dailyPhotoGalleryDirectory =
                UserSettingsSingleton.CurrentSettings().LocalSiteDailyPhotoGalleryDirectory();

            var dailyGalleryFiles = dailyPhotoGalleryDirectory.GetFiles().OrderBy(x => x.Name).ToList();

            var allPhotoDays = (await db.PhotoContents.Select(x => x.PhotoCreatedOn).Distinct().ToListAsync())
                .Select(x => x.Date).Distinct().ToList();

            progress?.Report(
                $"Found {dailyGalleryFiles.Count} Daily Dates in the db, {allPhotoDays.Count} files in daily photo galleries.");

            foreach (var loopGalleryFiles in dailyGalleryFiles)
            {
                var dateTimeForFile = UserSettingsSingleton.CurrentSettings()
                    .LocalSiteDailyPhotoGalleryPhotoDateFromFileInfo(loopGalleryFiles);

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

        public static async Task RemovePhotoMediaArchiveFilesNotInCurrentDatabase(IProgress<string> progress)
        {
            progress?.Report("Starting Photo Media Archive Cleanup");

            var db = await Db.Context();
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

        public static async Task RemovePostDirectoriesNotFoundInCurrentDatabase(IProgress<string> progress)
        {
            progress?.Report("Starting Directory Cleanup");

            var db = await Db.Context();
            var dbFolders = db.PostContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();

            var siteTopLevelPostDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePostDirectory();
            var folderDirectories = siteTopLevelPostDirectory.GetDirectories().OrderBy(x => x.Name).ToList();

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

                    progress?.Report($"{loopExistingContentDirectories.FullName} matches current Post Content");
                }
            }

            progress?.Report("Ending Post Directory Cleanup");
        }

        /// <summary>
        ///     Verify or Create all top level folders for a site - includes both local only directories like the Media Archive and
        ///     the top level folders for the generated site.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static List<GenerationReturn> VerifyOrCreateAllTopLevelFolders(this UserSettings settings,
            IProgress<string> progress)
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
            return new List<GenerationReturn>
            {
                settings.LocalSiteDirectory().CreateIfItDoesNotExist(),
                settings.LocalSitePhotoDirectory().CreateIfItDoesNotExist(),
                settings.LocalSitePhotoGalleryDirectory().CreateIfItDoesNotExist(),
                settings.LocalSiteDailyPhotoGalleryDirectory().CreateIfItDoesNotExist(),
                settings.LocalSiteFileDirectory().CreateIfItDoesNotExist(),
                settings.LocalSiteImageDirectory().CreateIfItDoesNotExist(),
                settings.LocalSiteNoteDirectory().CreateIfItDoesNotExist(),
                settings.LocalSitePointDirectory().CreateIfItDoesNotExist(),
                settings.LocalSitePostDirectory().CreateIfItDoesNotExist(),
                settings.LocalSiteLinkDirectory().CreateIfItDoesNotExist(),
                settings.LocalSiteNoteDirectory().CreateIfItDoesNotExist(),
                settings.LocalSiteTagsDirectory().CreateIfItDoesNotExist()
            };
        }

        /// <summary>
        ///     Verify or Create the Media Archive Folders.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static List<GenerationReturn> VerifyOrCreateMediaArchiveFolders(this UserSettings settings)
        {
            return new List<GenerationReturn>
            {
                settings.LocalSiteMediaArchiveDirectory().CreateIfItDoesNotExist(),
                settings.LocalMediaArchivePhotoDirectory().CreateIfItDoesNotExist(),
                settings.LocalMediaArchiveImageDirectory().CreateIfItDoesNotExist(),
                settings.LocalMediaArchiveFileDirectory().CreateIfItDoesNotExist()
            };
        }

        public static void WriteSelectedFileContentFileToMediaArchive(FileInfo selectedFile)
        {
            var userSettings = UserSettingsSingleton.CurrentSettings();
            var destinationFileName = Path.Combine(userSettings.LocalMediaArchiveFileDirectory().FullName,
                selectedFile.Name);

            if (destinationFileName == selectedFile.FullName) return;

            var destinationFile = new FileInfo(destinationFileName);

            if (destinationFile.Exists) destinationFile.Delete();

            selectedFile.CopyTo(destinationFileName);
        }

        public static async Task<GenerationReturn> WriteSelectedFileContentFileToMediaArchive(FileInfo selectedFile,
            bool replaceExisting)
        {
            var userSettings = UserSettingsSingleton.CurrentSettings();

            var destinationFileName = Path.Combine(userSettings.LocalMediaArchiveFileDirectory().FullName,
                selectedFile.Name);
            if (destinationFileName == selectedFile.FullName && !replaceExisting)
                return await GenerationReturn.Success("File is already in Media Archive");

            var destinationFile = new FileInfo(destinationFileName);

            if (destinationFile.Exists) destinationFile.Delete();

            selectedFile.CopyTo(destinationFileName);

            return await GenerationReturn.Success("File is copied to Media Archive");
        }

        public static void WriteSelectedImageContentFileToMediaArchive(FileInfo selectedFile)
        {
            var userSettings = UserSettingsSingleton.CurrentSettings();
            var destinationFileName = Path.Combine(userSettings.LocalMediaArchiveImageDirectory().FullName,
                selectedFile.Name);

            if (destinationFileName == selectedFile.FullName) return;

            var destinationFile = new FileInfo(destinationFileName);

            if (destinationFile.Exists) destinationFile.Delete();

            selectedFile.CopyTo(destinationFileName);
        }

        public static async Task<GenerationReturn> WriteSelectedImageContentFileToMediaArchive(FileInfo selectedFile,
            bool replaceExisting)
        {
            var userSettings = UserSettingsSingleton.CurrentSettings();

            var destinationFileName = Path.Combine(userSettings.LocalMediaArchiveImageDirectory().FullName,
                selectedFile.Name);
            if (destinationFileName == selectedFile.FullName && !replaceExisting)
                return await GenerationReturn.Success("Image is already in Media Archive");

            var destinationFile = new FileInfo(destinationFileName);

            if (destinationFile.Exists) destinationFile.Delete();

            selectedFile.CopyTo(destinationFileName);

            return await GenerationReturn.Success("Image is copied to Media Archive");
        }

        public static void WriteSelectedPhotoContentFileToMediaArchive(FileInfo selectedFile)
        {
            var userSettings = UserSettingsSingleton.CurrentSettings();
            var destinationFileName = Path.Combine(userSettings.LocalMediaArchivePhotoDirectory().FullName,
                selectedFile.Name);

            if (destinationFileName == selectedFile.FullName) return;

            var destinationFile = new FileInfo(destinationFileName);

            if (destinationFile.Exists) destinationFile.Delete();

            selectedFile.CopyTo(destinationFileName);
        }

        public static async Task<GenerationReturn> WriteSelectedPhotoContentFileToMediaArchive(FileInfo selectedFile,
            bool replaceExisting)
        {
            var userSettings = UserSettingsSingleton.CurrentSettings();

            var destinationFileName = Path.Combine(userSettings.LocalMediaArchivePhotoDirectory().FullName,
                selectedFile.Name);
            if (destinationFileName == selectedFile.FullName && !replaceExisting)
                return await GenerationReturn.Success("Photo is already in Media Archive");

            var destinationFile = new FileInfo(destinationFileName);

            if (destinationFile.Exists) destinationFile.Delete();

            selectedFile.CopyTo(destinationFileName);

            return await GenerationReturn.Success("Photo is copied to Media Archive");
        }
    }
}