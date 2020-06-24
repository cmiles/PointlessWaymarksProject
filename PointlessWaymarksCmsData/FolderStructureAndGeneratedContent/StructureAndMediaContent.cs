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

            returnList.AddRange(GenerationReturn.FilterForErrors(db.ImageContents.ToList().Select(x =>
                GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSiteImageContentDirectory(x),
                    $"Check Content Folder for Image {x.Title}")).ToList()));

            returnList.AddRange(GenerationReturn.FilterForErrors(db.FileContents.ToList().Select(x =>
                GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSiteFileContentDirectory(x),
                    $"Check Content Folder for File {x.Title}")).ToList()));

            returnList.AddRange(GenerationReturn.FilterForErrors(db.NoteContents.ToList().Select(x =>
                GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSiteNoteContentDirectory(x),
                    $"Check Content Folder for Note {x.Title}")).ToList()));

            returnList.AddRange(GenerationReturn.FilterForErrors(db.PostContents.ToList().Select(x =>
                GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSitePostContentDirectory(x),
                    $"Check Content Folder for Post {x.Title}")).ToList()));

            returnList.AddRange(GenerationReturn.FilterForErrors(db.PhotoContents.ToList().Select(x =>
                GenerationReturn.TryCatchToGenerationReturn(() => settings.LocalSitePhotoContentDirectory(x),
                    $"Check Content Folder for Photo {x.Title}")).ToList()));

            return returnList;
        }

        public static GenerationReturn CheckFileOriginalFileIsInMediaAndContentDirectories(FileContent dbContent,
            IProgress<string> progress)
        {
            if (dbContent == null)
                return new GenerationReturn
                {
                    HasError = true, ErrorNote = "Null File Content was submitted to the File File Check"
                };

            UserSettingsSingleton.CurrentSettings().VerifyOrCreateAllTopLevelFolders(progress);

            if (string.IsNullOrWhiteSpace(dbContent.OriginalFileName))
                return new GenerationReturn
                {
                    HasError = true,
                    ContentId = dbContent.ContentId,
                    ErrorNote = $"File {dbContent.Title} does not have an Original File assigned"
                };

            var archiveFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileDirectory().FullName,
                dbContent.OriginalFileName));

            var fileContentDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(dbContent);

            var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, dbContent.OriginalFileName));

            if (!archiveFile.Exists && !contentFile.Exists)
                return new GenerationReturn
                {
                    HasError = true,
                    ContentId = dbContent.ContentId,
                    ErrorNote = $"Neither {archiveFile.FullName} nor {contentFile.FullName} exists - " +
                                $"there appears to be a file missing for File Title {dbContent.Title} " +
                                $"slug {dbContent.Slug}"
                };

            if (archiveFile.Exists && !contentFile.Exists) archiveFile.CopyTo(contentFile.FullName);

            if (!archiveFile.Exists && contentFile.Exists) contentFile.CopyTo(archiveFile.FullName);

            archiveFile.Refresh();
            contentFile.Refresh();

            var bothFilesPresent = archiveFile.Exists && contentFile.Exists;

            if (bothFilesPresent)
                return GenerationReturn.NoError();

            return new GenerationReturn
            {
                HasError = true,
                ContentId = dbContent.ContentId,
                ErrorNote = $"There was a problem - Archive File Present: {archiveFile.Exists}, " +
                            $"Content File Present {contentFile.Exists} - {archiveFile.FullName}; {contentFile.FullName}"
            };
        }

        public static GenerationReturn CheckImageOriginalFileIsInMediaAndContentDirectories(ImageContent dbContent,
            IProgress<string> progress)
        {
            if (dbContent == null)
                return new GenerationReturn
                {
                    HasError = true, ErrorNote = "Null Image Content was submitted to the Image File Check"
                };

            UserSettingsSingleton.CurrentSettings().VerifyOrCreateAllTopLevelFolders(progress);

            if (string.IsNullOrWhiteSpace(dbContent.OriginalFileName))
                return new GenerationReturn
                {
                    HasError = true,
                    ContentId = dbContent.ContentId,
                    ErrorNote = $"Image {dbContent.Title} does not have an Original File assigned"
                };

            var archiveFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageDirectory().FullName,
                dbContent.OriginalFileName));

            var imageContentDirectory =
                UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(dbContent);

            var contentFile = new FileInfo(Path.Combine(imageContentDirectory.FullName, dbContent.OriginalFileName));

            if (!archiveFile.Exists && !contentFile.Exists)
                return new GenerationReturn
                {
                    HasError = true,
                    ContentId = dbContent.ContentId,
                    ErrorNote = $"Neither {archiveFile.FullName} nor {contentFile.FullName} exists - " +
                                $"there appears to be a file missing for Image Title {dbContent.Title} " +
                                $"slug {dbContent.Slug}"
                };

            if (archiveFile.Exists && !contentFile.Exists) archiveFile.CopyTo(contentFile.FullName);

            if (!archiveFile.Exists && contentFile.Exists) contentFile.CopyTo(archiveFile.FullName);

            archiveFile.Refresh();
            contentFile.Refresh();

            var bothFilesPresent = archiveFile.Exists && contentFile.Exists;

            if (bothFilesPresent)
                return GenerationReturn.NoError();

            return new GenerationReturn
            {
                HasError = true,
                ContentId = dbContent.ContentId,
                ErrorNote = $"There was a problem - Archive File Present: {archiveFile.Exists}, " +
                            $"Content File Present {contentFile.Exists} - {archiveFile.FullName}; {contentFile.FullName}"
            };
        }

        public static GenerationReturn CheckPhotoOriginalFileIsInMediaAndContentDirectories(PhotoContent dbContent,
            IProgress<string> progress)
        {
            if (dbContent == null)
                return new GenerationReturn
                {
                    HasError = true, ErrorNote = "Null Photo Content was submitted to the Photo File Check"
                };

            UserSettingsSingleton.CurrentSettings().VerifyOrCreateAllTopLevelFolders(progress);

            if (string.IsNullOrWhiteSpace(dbContent.OriginalFileName))
                return new GenerationReturn
                {
                    HasError = true,
                    ContentId = dbContent.ContentId,
                    ErrorNote = $"Photo {dbContent.Title} does not have an Original File assigned"
                };

            var archiveFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoDirectory().FullName,
                dbContent.OriginalFileName));

            var photoContentDirectory =
                UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(dbContent);

            var contentFile = new FileInfo(Path.Combine(photoContentDirectory.FullName, dbContent.OriginalFileName));

            if (!archiveFile.Exists && !contentFile.Exists)
                return new GenerationReturn
                {
                    HasError = true,
                    ContentId = dbContent.ContentId,
                    ErrorNote = $"Neither {archiveFile.FullName} nor {contentFile.FullName} exists - " +
                                $"there appears to be a file missing for Photo Title {dbContent.Title} " +
                                $"slug {dbContent.Slug}"
                };

            if (archiveFile.Exists && !contentFile.Exists) archiveFile.CopyTo(contentFile.FullName);

            if (!archiveFile.Exists && contentFile.Exists) contentFile.CopyTo(archiveFile.FullName);

            archiveFile.Refresh();
            contentFile.Refresh();

            var bothFilesPresent = archiveFile.Exists && contentFile.Exists;

            if (bothFilesPresent)
                return GenerationReturn.NoError();

            return new GenerationReturn
            {
                HasError = true,
                ContentId = dbContent.ContentId,
                ErrorNote = $"There was a problem - Archive File Present: {archiveFile.Exists}, " +
                            $"Content File Present {contentFile.Exists} - {archiveFile.FullName}; {contentFile.FullName}"
            };
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

                returnList.Add(PictureResizing.CopyCleanResizeImage(loopItem, progress));

                loopCount++;
            }

            return GenerationReturn.FilterForErrors(returnList);
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

                returnList.Add(PictureResizing.CopyCleanResizePhoto(loopItem, progress));

                loopCount++;
            }

            return GenerationReturn.FilterForErrors(returnList);
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

                returnList.Add(CheckFileOriginalFileIsInMediaAndContentDirectories(loopItem, progress));

                loopCount++;
            }

            return GenerationReturn.FilterForErrors(returnList);
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
            return GenerationReturn.FilterForErrors(new List<List<GenerationReturn>>
            {
                settings.VerifyOrCreateMediaArchiveFolders(), settings.VerifyOrCreateLocalTopLevelSiteFolders()
            });
        }

        /// <summary>
        ///     Verify or Create top level folders needed for the Generated Site
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static List<GenerationReturn> VerifyOrCreateLocalTopLevelSiteFolders(this UserSettings settings)
        {
            return GenerationReturn.FilterForErrors(new List<GenerationReturn>
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
            });
        }

        /// <summary>
        ///     Verify or Create the Media Archive Folders.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static List<GenerationReturn> VerifyOrCreateMediaArchiveFolders(this UserSettings settings)
        {
            return GenerationReturn.FilterForErrors(new List<GenerationReturn>
            {
                settings.LocalSiteMediaArchiveDirectory().CreateIfItDoesNotExist(),
                settings.LocalMediaArchivePhotoDirectory().CreateIfItDoesNotExist(),
                settings.LocalMediaArchiveImageDirectory().CreateIfItDoesNotExist(),
                settings.LocalMediaArchiveFileDirectory().CreateIfItDoesNotExist()
            });
        }
    }
}