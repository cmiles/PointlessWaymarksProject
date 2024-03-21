using System.Text.Json;
using System.Text.RegularExpressions;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using Serilog;

namespace PointlessWaymarks.CmsData.Json;

public static partial class Import
{
    public static List<T> ContentFromFiles<T>(List<string> fileList)
    {
        var returnList = new List<T>();

        foreach (var loopFiles in fileList)
        {
            var deserialized = JsonSerializer.Deserialize<T>(File.ReadAllText(loopFiles));
            if (deserialized != null) returnList.Add(deserialized);
        }

        return returnList;
    }

    public static List<T> ContentFromFilesWithPrefixFilter<T>(List<string> fileLists, string fileIdentifierPrefix)
    {
        var contentFiles = fileLists.Where(x => x.Contains($"\\{fileIdentifierPrefix}")).ToList();

        var returnList = new List<T>();

        foreach (var loopFiles in contentFiles)
        {
            var deserialized = JsonSerializer.Deserialize<T>(File.ReadAllText(loopFiles));
            if (deserialized != null) returnList.Add(deserialized);
        }

        return returnList;
    }

    public static async Task FullImportFromRootDirectory(DirectoryInfo? rootDirectory,
        IProgress<string>? progress = null)
    {
        if (rootDirectory is not { Exists: true })
        {
            progress?.Report("Root Directory does not exist?");
            return;
        }

        var contentDataJson = GetAllContentDataDirectoryJsonFiles(rootDirectory, progress);

        var groupedContentData = contentDataJson.GroupBy(x => x.type).OrderBy(x => x.Key).ToList();

        //!!Content List
        foreach (var loopContentGroup in groupedContentData)
            switch (loopContentGroup.Key)
            {
                case Db.ContentTypeDisplayStringForFile:
                    await DbImport.FileContentToDb(
                        ContentFromFiles<FileContentOnDiskData>(loopContentGroup.Select(x => x.fullFileName).ToList())
                            .Select(x => x.Content).ToList(), progress);
                    break;
                case Db.ContentTypeDisplayStringForGeoJson:
                    await DbImport.GeoJsonContentToDb(
                        ContentFromFiles<GeoJsonContentOnDiskData>(
                            loopContentGroup.Select(x => x.fullFileName).ToList()).Select(x => x.Content).ToList(),
                        progress);
                    break;
                case Db.ContentTypeDisplayStringForImage:
                    await DbImport.ImageContentToDb(
                        ContentFromFiles<ImageContentOnDiskData>(loopContentGroup.Select(x => x.fullFileName).ToList())
                            .Select(x => x.Content).ToList(), progress);
                    break;
                case Db.ContentTypeDisplayStringForLine:
                    await DbImport.LineContentToDb(
                        ContentFromFiles<LineContentOnDiskData>(loopContentGroup.Select(x => x.fullFileName).ToList())
                            .Select(x => x.Content).ToList(), progress);
                    break;
                case Db.ContentTypeDisplayStringForMap:
                    await DbImport.MapComponentToDb(
                        ContentFromFiles<MapComponentOnDiskData>(loopContentGroup.Select(x => x.fullFileName).ToList())
                            .Select(x => x.Content).ToList(), progress);
                    break;
                case Db.ContentTypeDisplayStringForNote:
                    await DbImport.NoteContentToDb(
                        ContentFromFiles<NoteContentOnDiskData>(loopContentGroup.Select(x => x.fullFileName).ToList())
                            .Select(x => x.Content).ToList(), progress);
                    break;
                case Db.ContentTypeDisplayStringForPhoto:
                    await DbImport.PhotoContentToDb(
                        ContentFromFiles<PhotoContentOnDiskData>(loopContentGroup.Select(x => x.fullFileName).ToList())
                            .Select(x => x.Content).ToList(), progress);
                    break;
                case Db.ContentTypeDisplayStringForPoint:
                    await DbImport.PointContentToDb(
                        ContentFromFiles<PointContentOnDiskData>(loopContentGroup.Select(x => x.fullFileName).ToList())
                            .Select(x => x.Content).ToList(), progress);
                    break;
                case Db.ContentTypeDisplayStringForPost:
                    await DbImport.PostContentToDb(
                        ContentFromFiles<PostContentOnDiskData>(loopContentGroup.Select(x => x.fullFileName).ToList())
                            .Select(x => x.Content).ToList(), progress);
                    break;
                case Db.ContentTypeDisplayStringForVideo:
                    await DbImport.VideoContentToDb(
                        ContentFromFiles<VideoContentOnDiskData>(loopContentGroup.Select(x => x.fullFileName).ToList())
                            .Select(x => x.Content).ToList(), progress);
                    break;
            }

        var nonContentDataFiles = GetAllNonContentDataDirectoryJsonFiles(rootDirectory, progress);

        await DbImport.LinkContentToDb(
            ContentFromFilesWithPrefixFilter<List<LinkContent>>(nonContentDataFiles,
                    Path.GetFileNameWithoutExtension(UserSettingsSingleton.CurrentSettings().LocalSiteLinkListJsonFile()
                        .FullName))
                .SelectMany(x => x).ToList(),
            progress);

        await DbImport.MapIconsDb(
            ContentFromFilesWithPrefixFilter<List<MapIcon>>(nonContentDataFiles,
                    Path.GetFileNameWithoutExtension(UserSettingsSingleton.CurrentSettings().LocalSiteMapIconsDataFile()
                        .FullName))
                .SelectMany(x => x).ToList(),
            progress);

        MenuLinksToDb(
            ContentFromFilesWithPrefixFilter<MenuLink>(nonContentDataFiles,
                Path.GetFileNameWithoutExtension(UserSettingsSingleton.CurrentSettings().LocalSiteMenuLinksJsonFile()
                    .FullName)),
            progress);
        TagExclusionsToDb(
            ContentFromFilesWithPrefixFilter<TagExclusion>(nonContentDataFiles,
                Path.GetFileNameWithoutExtension(UserSettingsSingleton.CurrentSettings()
                    .LocalSiteTagExclusionsJsonFile().FullName)), progress);
        TagExclusionsToDb(
            ContentFromFilesWithPrefixFilter<TagExclusion>(nonContentDataFiles,
                Path.GetFileNameWithoutExtension(UserSettingsSingleton.CurrentSettings()
                    .LocalSiteTagExclusionsJsonFile().FullName)), progress);


        await DbImport.HistoricFileContentToDb(
            ContentFromFilesWithPrefixFilter<List<HistoricFileContent>>(nonContentDataFiles,
                    UserSettingsUtilities.HistoricFileContentPrefix)
                .SelectMany(x => x).ToList(), progress);
        await DbImport.HistoricGeoJsonContentToDb(
            ContentFromFilesWithPrefixFilter<List<HistoricGeoJsonContent>>(nonContentDataFiles,
                    UserSettingsUtilities.HistoricGeoJsonContentPrefix)
                .SelectMany(x => x).ToList(), progress);
        await DbImport.HistoricImageContentToDb(
            ContentFromFilesWithPrefixFilter<List<HistoricImageContent>>(nonContentDataFiles,
                    UserSettingsUtilities.HistoricImageContentPrefix)
                .SelectMany(x => x).ToList(), progress);
        await DbImport.HistoricLineContentToDb(
            ContentFromFilesWithPrefixFilter<List<HistoricLineContent>>(nonContentDataFiles,
                    UserSettingsUtilities.HistoricLineContentPrefix)
                .SelectMany(x => x).ToList(), progress);
        await DbImport.HistoricLinkContentToDb(
            ContentFromFilesWithPrefixFilter<List<HistoricLinkContent>>(nonContentDataFiles,
                    UserSettingsUtilities.HistoricLinkListFileName).SelectMany(x => x)
                .ToList(), progress);
        await DbImport.HistoricNoteContentToDb(
            ContentFromFilesWithPrefixFilter<List<HistoricNoteContent>>(nonContentDataFiles,
                    UserSettingsUtilities.HistoricNoteContentPrefix)
                .SelectMany(x => x).ToList(), progress);
        await DbImport.HistoricPointContentToDb(
            ContentFromFilesWithPrefixFilter<List<HistoricPointContent>>(nonContentDataFiles,
                    UserSettingsUtilities.HistoricPointContentPrefix)
                .SelectMany(x => x).ToList(), progress);
        await DbImport.HistoricPostContentToDb(
            ContentFromFilesWithPrefixFilter<List<HistoricPostContent>>(nonContentDataFiles,
                    UserSettingsUtilities.HistoricPostContentPrefix)
                .SelectMany(x => x).ToList(), progress);
        await DbImport.HistoricPhotoContentToDb(
            ContentFromFilesWithPrefixFilter<List<HistoricPhotoContent>>(nonContentDataFiles,
                    UserSettingsUtilities.HistoricPhotoContentPrefix)
                .SelectMany(x => x).ToList(), progress);
        await DbImport.HistoricVideoContentToDb(
            ContentFromFilesWithPrefixFilter<List<HistoricVideoContent>>(nonContentDataFiles,
                    UserSettingsUtilities.HistoricVideoContentPrefix)
                .SelectMany(x => x).ToList(), progress);
    }


    public static List<(string type, string fullFileName)> GetAllContentDataDirectoryJsonFiles(
        DirectoryInfo rootDirectory,
        IProgress<string>? progress = null)
    {
        var contentDataDirectory = new DirectoryInfo(Path.Combine(rootDirectory.FullName,
            UserSettingsSingleton.CurrentSettings().LocalSiteContentDataDirectory().Name));

        if (!contentDataDirectory.Exists)
        {
            progress?.Report("Content Data Directory does not exist?");
            return [];
        }

        var allFiles = Directory.GetFiles(rootDirectory.FullName, "*.json", SearchOption.AllDirectories).ToList();
        List<(string type, string fullFileName)> returnList = [];

        var fileCount = 0;
        foreach (var loopFile in allFiles)
        {
            if (fileCount++ % 100 == 0)
                progress?.Report(
                    $"Checking File {fileCount} of {allFiles.Count} for Content Type - Found {returnList.Count} Files So Far...");

            using var fs = new FileStream(loopFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            var buffer = new char[256];
            var numRead = sr.Read(buffer, 0, buffer.Length);
            var firstUpTo256Chars = new string(buffer, 0, numRead);

            var match = OnDiskDataContentTypeRegex().Match(firstUpTo256Chars);
            if (match.Success)
            {
                var contentTypeValue = match.Groups["ContentTypeValue"].Value;
                if (!string.IsNullOrWhiteSpace(contentTypeValue)) returnList.Add((contentTypeValue, loopFile));
            }
            else
            {
                Log.Debug("Json OnDiskData Import - File {jsonFile} did not have a Content Type Match", loopFile);
            }
        }

        return returnList;
    }

    public static List<string> GetAllNonContentDataDirectoryJsonFiles(
        DirectoryInfo rootDirectory,
        IProgress<string>? progress = null)
    {
        if (!rootDirectory.Exists)
        {
            progress?.Report("Content Data Directory does not exist?");
            return [];
        }

        var returnList = rootDirectory.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly).Select(x => x.FullName)
            .ToList();

        var topLevelDirectoriesToScan = rootDirectory.GetDirectories().Where(x =>
            x.Name != UserSettingsSingleton.CurrentSettings().LocalSiteContentDataDirectory().Name);

        foreach (var loopDirectory in topLevelDirectoriesToScan)
            returnList.AddRange(loopDirectory.EnumerateFiles("*.json", SearchOption.AllDirectories)
                .Select(x => x.FullName));

        return returnList;
    }

    public static void MenuLinksToDb(List<MenuLink>? toImport, IProgress<string>? progress = null)
    {
        progress?.Report("MenuLinks - Starting");

        if (toImport == null || !toImport.Any())
        {
            progress?.Report("No MenuLink items to import...");
            return;
        }

        progress?.Report($"MenuLinks - Working with {toImport.Count} Entries");

        var db = Db.Context().Result;

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.MenuOrder} - Starting MenuLinks");

            var exactMatch = db.MenuLinks.Any(x =>
                x.MenuOrder == loopImportItem.MenuOrder && x.LinkTag == loopImportItem.LinkTag);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.MenuOrder} - Found exact match in DB - skipping");
                continue;
            }

            var orderMatch = db.MenuLinks.Any(x => x.MenuOrder == loopImportItem.MenuOrder);

            if (orderMatch)
            {
                progress?.Report(
                    $"{loopImportItem.MenuOrder} - Found a conflicting order match in DB - adding to the end");
                var maxOrder = db.MenuLinks.Max(x => x.MenuOrder);

                db.MenuLinks.Add(new MenuLink { LinkTag = loopImportItem.LinkTag, MenuOrder = maxOrder });
                db.SaveChanges(true);
                continue;
            }

            progress?.Report($"{loopImportItem.MenuOrder} - Adding");

            db.MenuLinks.Add(new MenuLink { LinkTag = loopImportItem.LinkTag, MenuOrder = loopImportItem.MenuOrder });
            db.SaveChanges(true);

            progress?.Report($"{loopImportItem.MenuOrder} - Imported");
        }

        progress?.Report("MenuLinks - Finished");
    }


    [GeneratedRegex(@"""ContentType""\s*:\s*""(?<ContentTypeValue>.*)""")]
    private static partial Regex OnDiskDataContentTypeRegex();

    public static void TagExclusionsToDb(List<TagExclusion>? toImport, IProgress<string>? progress = null)
    {
        progress?.Report("TagExclusions - Starting");

        if (toImport == null || !toImport.Any())
        {
            progress?.Report("No TagExclusion items to import...");
            return;
        }

        progress?.Report($"TagExclusions - Working with {toImport.Count} Entries");

        var db = Db.Context().Result;

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Tag} - Starting TagExclusions");

            var exactMatch = db.TagExclusions.Any(x => x.Tag == loopImportItem.Tag);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Tag} - Found exact match in DB - skipping");
                continue;
            }

            progress?.Report($"{loopImportItem.Tag} - Adding");

            db.TagExclusions.Add(new TagExclusion { Tag = loopImportItem.Tag });
            db.SaveChanges(true);

            progress?.Report($"{loopImportItem.Tag} - Imported");
        }

        progress?.Report("TagExclusions - Finished");
    }
}