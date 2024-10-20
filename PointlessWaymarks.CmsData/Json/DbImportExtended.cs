using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Json;

public static partial class DbImport
{
    public static async Task HistoricPointContentToDb(List<HistoricPointContent> toImport,
        IProgress<string>? progress = null)
    {
        progress?.Report("HistoricPointContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No items to import?");
            return;
        }

        progress?.Report($"HistoricPointContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        var currentLoop = 1;
        var totalCount = toImport.Count;

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

            if (await db.HistoricPointContents.AnyAsync(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
            {
                progress?.Report($"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                continue;
            }

            loopImportItem.Id = 0;

            db.HistoricPointContents.Add(loopImportItem);

            await db.SaveChangesAsync(true);
        }

        progress?.Report("PointContent - Finished");
    }


    public static async Task MapComponentToDb(List<MapComponentDto> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("MapComponent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No MapComponent items to import...");
            return;
        }

        progress?.Report($"MapComponent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - Starting MapComponent");

            var exactMatch = await db.MapComponents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.MapComponents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (db.HistoricMapComponents.Any(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic MapComponent");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricMapComponents");

                    var newHistoricEntry = new HistoricMapComponent();
                    newHistoricEntry.InjectFrom(loopImportItem);
                    newHistoricEntry.Id = 0;

                    db.HistoricMapComponents.Add(newHistoricEntry);
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SaveMapComponent(loopImportItem);
        }

        progress?.Report("MapComponent - Finished");
    }

    public static async Task MapIconsDb(List<MapIcon> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("MapIcon - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No MapIcon items to import...");
            return;
        }

        progress?.Report($"MapIcon - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.IconName} - Starting MapIcon");

            var exactMatch = await db.MapIcons.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.IconName} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.MapIcons.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (await db.HistoricMapIcons.AnyAsync(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.IconName} - Found later entry in Db and this entry already in Historic MapIcon");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.IconName} - Found later entry already in db - moving this version to HistoricMapIconContents");

                    var newHistoricEntry = loopImportItem.ToHistoricMapIcon();

                    db.HistoricMapIcons.Add(newHistoricEntry);
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SaveMapIcon(loopImportItem);
        }

        progress?.Report("MapIconContent - Finished");
    }

    public static async Task PointContentToDb(List<PointContentDto> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("PointContent - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No PointContent items to import...");
            return;
        }

        progress?.Report($"PointContent - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - Starting PointContent");

            var exactMatch = await db.PointContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.PointContents.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (await db.HistoricPointContents.AnyAsync(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic PointContent");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricPointContents");

                    db.HistoricPointContents.Add(loopImportItem.ToHistoricDbObject());
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SavePointContent(loopImportItem);
        }

        progress?.Report("PointContent - Finished");
    }


    public static async Task SnippetToDb(List<Snippet> toImport, IProgress<string>? progress = null)
    {
        progress?.Report("Snippet - Starting");

        if (!toImport.Any())
        {
            progress?.Report("No Snippet items to import...");
            return;
        }

        progress?.Report($"Snippet - Working with {toImport.Count} Entries");

        var db = await Db.Context();

        foreach (var loopImportItem in toImport)
        {
            progress?.Report($"{loopImportItem.Title} - Starting Snippet");

            var exactMatch = await db.Snippets.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

            if (exactMatch)
            {
                progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                continue;
            }

            var laterEntries = await db.Snippets.AnyAsync(x =>
                x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

            if (laterEntries)
            {
                if (await db.HistoricSnippets.AnyAsync(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic Snippet");
                }
                else
                {
                    progress?.Report(
                        $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricSnippets");

                    var newHistoricEntry = new HistoricSnippet
                    {
                        ContentId = loopImportItem.ContentId,
                        ContentVersion = loopImportItem.ContentVersion,
                        CreatedOn = loopImportItem.CreatedOn
                    };
                    newHistoricEntry.InjectFrom(loopImportItem);
                    newHistoricEntry.Id = 0;

                    db.HistoricSnippets.Add(newHistoricEntry);
                    await db.SaveChangesAsync(true);
                }

                continue;
            }

            await Db.SaveSnippet(loopImportItem);
        }

        progress?.Report("Snippet - Finished");
    }
}