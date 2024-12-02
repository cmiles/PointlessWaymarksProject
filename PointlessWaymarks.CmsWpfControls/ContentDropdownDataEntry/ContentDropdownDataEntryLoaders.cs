using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;

namespace PointlessWaymarks.CmsWpfControls.ContentDropdownDataEntry;

public static class ContentDropdownDataEntryLoaders
{
    public static async Task<List<ContentDropdownDataChoice>> GetCurrentLineEntries()
    {
        var db = await Db.Context();

        var lines = await db.LineContents.Select(x => new { x.Title, x.ContentId }).OrderBy(x => x.Title)
            .ToListAsync();
        var lineChoices = lines.Select(x => new ContentDropdownDataChoice
            { ContentId = x.ContentId, DisplayString = x.Title ?? "Unknown" }).ToList();

        return lineChoices;
    }

    public static async Task<List<ContentDropdownDataChoice>> GetCurrentMapEntries()
    {
        var db = await Db.Context();

        var maps = await db.MapComponents.Select(x => new { x.Title, x.ContentId }).OrderBy(x => x.Title)
            .ToListAsync();
        var mapChoices = maps.Select(x => new ContentDropdownDataChoice
            { ContentId = x.ContentId, DisplayString = x.Title ?? "Unknown" }).ToList();

        return mapChoices;
    }

    public static async Task<List<ContentDropdownDataChoice>> GetCurrentPointEntries()
    {
        var db = await Db.Context();

        var points = await db.PointContents.Select(x => new { x.Title, x.ContentId }).OrderBy(x => x.Title)
            .ToListAsync();
        var pointChoices = points.Select(x => new ContentDropdownDataChoice
            { ContentId = x.ContentId, DisplayString = x.Title ?? "Unknown" }).ToList();

        return pointChoices;
    }
}