
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Json
{
    public static partial class DbImport
    {
    public static async Task FileContentToDb(List<FileContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("FileContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No FileContent items to import...");
            return;
        }

        progress?.Report($"FileContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - Starting FileContent");

            var exactMatch = await db.FileContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.FileContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (await db.HistoricFileContents.AnyAsync(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic FileContent");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricFileContents");

                    var newHistoricEntry = new HistoricFileContent();
                    newHistoricEntry.InjectFrom(loopImportItem);
                    newHistoricEntry.Id = 0;

                    db.HistoricFileContents.Add(newHistoricEntry);
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SaveFileContent(loopImportItem);
        }

        progress?.Report("FileContent - Finished");
    }
    public static async Task GeoJsonContentToDb(List<GeoJsonContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("GeoJsonContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No GeoJsonContent items to import...");
            return;
        }

        progress?.Report($"GeoJsonContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - Starting GeoJsonContent");

            var exactMatch = await db.GeoJsonContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.GeoJsonContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (await db.HistoricGeoJsonContents.AnyAsync(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic GeoJsonContent");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricGeoJsonContents");

                    var newHistoricEntry = new HistoricGeoJsonContent();
                    newHistoricEntry.InjectFrom(loopImportItem);
                    newHistoricEntry.Id = 0;

                    db.HistoricGeoJsonContents.Add(newHistoricEntry);
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SaveGeoJsonContent(loopImportItem);
        }

        progress?.Report("GeoJsonContent - Finished");
    }
    public static async Task ImageContentToDb(List<ImageContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("ImageContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No ImageContent items to import...");
            return;
        }

        progress?.Report($"ImageContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - Starting ImageContent");

            var exactMatch = await db.ImageContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.ImageContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (await db.HistoricImageContents.AnyAsync(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic ImageContent");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricImageContents");

                    var newHistoricEntry = new HistoricImageContent();
                    newHistoricEntry.InjectFrom(loopImportItem);
                    newHistoricEntry.Id = 0;

                    db.HistoricImageContents.Add(newHistoricEntry);
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SaveImageContent(loopImportItem);
        }

        progress?.Report("ImageContent - Finished");
    }
    public static async Task LineContentToDb(List<LineContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("LineContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No LineContent items to import...");
            return;
        }

        progress?.Report($"LineContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - Starting LineContent");

            var exactMatch = await db.LineContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.LineContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (await db.HistoricLineContents.AnyAsync(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic LineContent");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricLineContents");

                    var newHistoricEntry = new HistoricLineContent();
                    newHistoricEntry.InjectFrom(loopImportItem);
                    newHistoricEntry.Id = 0;

                    db.HistoricLineContents.Add(newHistoricEntry);
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SaveLineContent(loopImportItem);
        }

        progress?.Report("LineContent - Finished");
    }
    public static async Task LinkContentToDb(List<LinkContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("LinkContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No LinkContent items to import...");
            return;
        }

        progress?.Report($"LinkContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - Starting LinkContent");

            var exactMatch = await db.LinkContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.LinkContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (await db.HistoricLinkContents.AnyAsync(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic LinkContent");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricLinkContents");

                    var newHistoricEntry = new HistoricLinkContent();
                    newHistoricEntry.InjectFrom(loopImportItem);
                    newHistoricEntry.Id = 0;

                    db.HistoricLinkContents.Add(newHistoricEntry);
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SaveLinkContent(loopImportItem);
        }

        progress?.Report("LinkContent - Finished");
    }
    public static async Task NoteContentToDb(List<NoteContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("NoteContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No NoteContent items to import...");
            return;
        }

        progress?.Report($"NoteContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - Starting NoteContent");

            var exactMatch = await db.NoteContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.NoteContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (await db.HistoricNoteContents.AnyAsync(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic NoteContent");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricNoteContents");

                    var newHistoricEntry = new HistoricNoteContent();
                    newHistoricEntry.InjectFrom(loopImportItem);
                    newHistoricEntry.Id = 0;

                    db.HistoricNoteContents.Add(newHistoricEntry);
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SaveNoteContent(loopImportItem);
        }

        progress?.Report("NoteContent - Finished");
    }
    public static async Task PhotoContentToDb(List<PhotoContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("PhotoContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No PhotoContent items to import...");
            return;
        }

        progress?.Report($"PhotoContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - Starting PhotoContent");

            var exactMatch = await db.PhotoContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.PhotoContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (await db.HistoricPhotoContents.AnyAsync(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic PhotoContent");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricPhotoContents");

                    var newHistoricEntry = new HistoricPhotoContent();
                    newHistoricEntry.InjectFrom(loopImportItem);
                    newHistoricEntry.Id = 0;

                    db.HistoricPhotoContents.Add(newHistoricEntry);
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SavePhotoContent(loopImportItem);
        }

        progress?.Report("PhotoContent - Finished");
    }
    public static async Task PostContentToDb(List<PostContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("PostContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No PostContent items to import...");
            return;
        }

        progress?.Report($"PostContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - Starting PostContent");

            var exactMatch = await db.PostContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.PostContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (await db.HistoricPostContents.AnyAsync(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic PostContent");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricPostContents");

                    var newHistoricEntry = new HistoricPostContent();
                    newHistoricEntry.InjectFrom(loopImportItem);
                    newHistoricEntry.Id = 0;

                    db.HistoricPostContents.Add(newHistoricEntry);
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SavePostContent(loopImportItem);
        }

        progress?.Report("PostContent - Finished");
    }
    public static async Task TrailContentToDb(List<TrailContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("TrailContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No TrailContent items to import...");
            return;
        }

        progress?.Report($"TrailContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - Starting TrailContent");

            var exactMatch = await db.TrailContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.TrailContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (await db.HistoricTrailContents.AnyAsync(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic TrailContent");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricTrailContents");

                    var newHistoricEntry = new HistoricTrailContent();
                    newHistoricEntry.InjectFrom(loopImportItem);
                    newHistoricEntry.Id = 0;

                    db.HistoricTrailContents.Add(newHistoricEntry);
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SaveTrailContent(loopImportItem);
        }

        progress?.Report("TrailContent - Finished");
    }
    public static async Task VideoContentToDb(List<VideoContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("VideoContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No VideoContent items to import...");
            return;
        }

        progress?.Report($"VideoContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - Starting VideoContent");

            var exactMatch = await db.VideoContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.VideoContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (await db.HistoricVideoContents.AnyAsync(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic VideoContent");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricVideoContents");

                    var newHistoricEntry = new HistoricVideoContent();
                    newHistoricEntry.InjectFrom(loopImportItem);
                    newHistoricEntry.Id = 0;

                    db.HistoricVideoContents.Add(newHistoricEntry);
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SaveVideoContent(loopImportItem);
        }

        progress?.Report("VideoContent - Finished");
    }

    public static async Task HistoricFileContentToDb(List<HistoricFileContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("HistoricFileContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No items to import?");
            return;
        }

        progress?.Report($"HistoricFileContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        var currentLoop = 1;
        var totalCount = toImport.Count;

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

            if (await db.HistoricFileContents.AnyAsync(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
            {
                progress?.Report($"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                continue;
            }

            loopImportItem.Id = 0;

            db.HistoricFileContents.Add(loopImportItem);

            await db.SaveChangesAsync(true);
        }

        progress?.Report("HistoricFileContent - Finished");
    }
    public static async Task HistoricGeoJsonContentToDb(List<HistoricGeoJsonContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("HistoricGeoJsonContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No items to import?");
            return;
        }

        progress?.Report($"HistoricGeoJsonContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        var currentLoop = 1;
        var totalCount = toImport.Count;

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

            if (await db.HistoricGeoJsonContents.AnyAsync(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
            {
                progress?.Report($"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                continue;
            }

            loopImportItem.Id = 0;

            db.HistoricGeoJsonContents.Add(loopImportItem);

            await db.SaveChangesAsync(true);
        }

        progress?.Report("HistoricGeoJsonContent - Finished");
    }
    public static async Task HistoricImageContentToDb(List<HistoricImageContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("HistoricImageContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No items to import?");
            return;
        }

        progress?.Report($"HistoricImageContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        var currentLoop = 1;
        var totalCount = toImport.Count;

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

            if (await db.HistoricImageContents.AnyAsync(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
            {
                progress?.Report($"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                continue;
            }

            loopImportItem.Id = 0;

            db.HistoricImageContents.Add(loopImportItem);

            await db.SaveChangesAsync(true);
        }

        progress?.Report("HistoricImageContent - Finished");
    }
    public static async Task HistoricLineContentToDb(List<HistoricLineContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("HistoricLineContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No items to import?");
            return;
        }

        progress?.Report($"HistoricLineContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        var currentLoop = 1;
        var totalCount = toImport.Count;

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

            if (await db.HistoricLineContents.AnyAsync(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
            {
                progress?.Report($"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                continue;
            }

            loopImportItem.Id = 0;

            db.HistoricLineContents.Add(loopImportItem);

            await db.SaveChangesAsync(true);
        }

        progress?.Report("HistoricLineContent - Finished");
    }
    public static async Task HistoricLinkContentToDb(List<HistoricLinkContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("HistoricLinkContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No items to import?");
            return;
        }

        progress?.Report($"HistoricLinkContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        var currentLoop = 1;
        var totalCount = toImport.Count;

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

            if (await db.HistoricLinkContents.AnyAsync(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
            {
                progress?.Report($"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                continue;
            }

            loopImportItem.Id = 0;

            db.HistoricLinkContents.Add(loopImportItem);

            await db.SaveChangesAsync(true);
        }

        progress?.Report("HistoricLinkContent - Finished");
    }
    public static async Task HistoricNoteContentToDb(List<HistoricNoteContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("HistoricNoteContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No items to import?");
            return;
        }

        progress?.Report($"HistoricNoteContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        var currentLoop = 1;
        var totalCount = toImport.Count;

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

            if (await db.HistoricNoteContents.AnyAsync(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
            {
                progress?.Report($"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                continue;
            }

            loopImportItem.Id = 0;

            db.HistoricNoteContents.Add(loopImportItem);

            await db.SaveChangesAsync(true);
        }

        progress?.Report("HistoricNoteContent - Finished");
    }
    public static async Task HistoricPhotoContentToDb(List<HistoricPhotoContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("HistoricPhotoContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No items to import?");
            return;
        }

        progress?.Report($"HistoricPhotoContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        var currentLoop = 1;
        var totalCount = toImport.Count;

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

            if (await db.HistoricPhotoContents.AnyAsync(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
            {
                progress?.Report($"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                continue;
            }

            loopImportItem.Id = 0;

            db.HistoricPhotoContents.Add(loopImportItem);

            await db.SaveChangesAsync(true);
        }

        progress?.Report("HistoricPhotoContent - Finished");
    }
    public static async Task HistoricPostContentToDb(List<HistoricPostContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("HistoricPostContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No items to import?");
            return;
        }

        progress?.Report($"HistoricPostContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        var currentLoop = 1;
        var totalCount = toImport.Count;

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

            if (await db.HistoricPostContents.AnyAsync(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
            {
                progress?.Report($"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                continue;
            }

            loopImportItem.Id = 0;

            db.HistoricPostContents.Add(loopImportItem);

            await db.SaveChangesAsync(true);
        }

        progress?.Report("HistoricPostContent - Finished");
    }
    public static async Task HistoricTrailContentToDb(List<HistoricTrailContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("HistoricTrailContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No items to import?");
            return;
        }

        progress?.Report($"HistoricTrailContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        var currentLoop = 1;
        var totalCount = toImport.Count;

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

            if (await db.HistoricTrailContents.AnyAsync(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
            {
                progress?.Report($"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                continue;
            }

            loopImportItem.Id = 0;

            db.HistoricTrailContents.Add(loopImportItem);

            await db.SaveChangesAsync(true);
        }

        progress?.Report("HistoricTrailContent - Finished");
    }
    public static async Task HistoricVideoContentToDb(List<HistoricVideoContent> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("HistoricVideoContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No items to import?");
            return;
        }

        progress?.Report($"HistoricVideoContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        var currentLoop = 1;
        var totalCount = toImport.Count;

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

            if (await db.HistoricVideoContents.AnyAsync(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
            {
                progress?.Report($"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                continue;
            }

            loopImportItem.Id = 0;

            db.HistoricVideoContents.Add(loopImportItem);

            await db.SaveChangesAsync(true);
        }

        progress?.Report("HistoricVideoContent - Finished");
    }
    }
}