using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.FolderStructureAndGeneratedContent
{
    public static class StructureAndMediaContent
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

        public static async Task PurgePhotoDirectoriesNotFoundInCurrentDatabase(IProgress<string> progress)
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
                    }

                    progress?.Report($"{loopExistingContentDirectories.FullName} matches current Photo Content");
                }
            }

            progress?.Report("Ending Directory Cleanup");

            //Daily photo purge - other
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
    }
}