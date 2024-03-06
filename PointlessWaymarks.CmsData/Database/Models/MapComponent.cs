using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PointlessWaymarks.CmsData.Database.Models;

public class MapComponent : IContentId, ICreatedAndLastUpdateOnAndBy, IUpdateNotes
{
    public double InitialViewBoundsMaxLatitude { get; set; }
    public double InitialViewBoundsMaxLongitude { get; set; }
    public double InitialViewBoundsMinLatitude { get; set; }
    public double InitialViewBoundsMinLongitude { get; set; }
    public string? Summary { get; set; }
    public string? Title { get; set; }
    public required Guid ContentId { get; set; }
    public required DateTime ContentVersion { get; set; }
    public int Id { get; set; }
    public string? CreatedBy { get; set; }
    public required DateTime CreatedOn { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
    [NotMapped] public DateTime LatestUpdate => LastUpdatedOn ?? CreatedOn;
    public string? UpdateNotes { get; set; }
    public string? UpdateNotesFormat { get; set; }

    public static MapComponent CreateInstance()
    {
        return NewContentModels.InitializeMapComponent(null);
    }

    public async Task<MapComponentDto> ToMapComponentDto(PointlessWaymarksContext db)
    {
        var elements = await db.MapComponentElements.Where(x => x.MapComponentContentId == ContentId).ToListAsync();

        return new MapComponentDto(this, elements);
    }
}