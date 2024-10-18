using System.Xml.Linq;
using PointlessWaymarks.CmsData.BracketCodes;

namespace PointlessWaymarks.CmsData.Database.Models;

public class MapElement
{
    public Guid ElementContentId { get; set; }
    public int Id { get; set; }
    public bool IncludeInDefaultView { get; set; }
    public bool IsFeaturedElement { get; set; }
    public string? LinksTo { get; set; } = string.Empty;
    public Guid MapComponentContentId { get; set; }
    public bool ShowDetailsDefault { get; set; }

    public async Task<string> LinkFromLinksTo()
    {
        if (string.IsNullOrWhiteSpace(LinksTo)) return string.Empty;

        if (LinksTo.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return LinksTo.Trim();

        var possibleContentIds = BracketCodeCommon.BracketCodeContentIds(LinksTo);

        if (!possibleContentIds.Any()) return string.Empty;

        var transformedLinksTo = await BracketCodeCommon.ProcessCodesForSite(LinksTo);
        if (string.IsNullOrWhiteSpace(transformedLinksTo)) return string.Empty;

        var url = XElement.Parse($"<p>{transformedLinksTo}</p>")
            .Descendants("a")
            .Select(x => x.Attribute("href")?.Value)
            .FirstOrDefault();

        return url ?? string.Empty;
    }
}