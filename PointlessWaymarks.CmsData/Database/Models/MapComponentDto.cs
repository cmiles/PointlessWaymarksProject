namespace PointlessWaymarks.CmsData.Database.Models;

public class MapComponentDto(MapComponent mapComponent, List<MapElement> elementList)
    : IContentId, ICreatedAndLastUpdateOnAndBy, IUpdateNotes
{
    public List<MapElement> Elements { get; set; } = elementList;
    public double InitialViewBoundsMaxLatitude { get; set; } = mapComponent.InitialViewBoundsMaxLatitude;
    public double InitialViewBoundsMaxLongitude { get; set; } = mapComponent.InitialViewBoundsMaxLongitude;
    public double InitialViewBoundsMinLatitude { get; set; } = mapComponent.InitialViewBoundsMinLatitude;
    public double InitialViewBoundsMinLongitude { get; set; } = mapComponent.InitialViewBoundsMinLongitude;
    public string? Summary { get; set; } = mapComponent.Summary;
    public string? Title { get; set; } = mapComponent.Title;
    public Guid ContentId { get; set; } = mapComponent.ContentId;
    public DateTime ContentVersion { get; set; } = mapComponent.ContentVersion;
    public int Id { get; set; } = mapComponent.Id;
    public string? CreatedBy { get; set; } = mapComponent.CreatedBy;
    public DateTime CreatedOn { get; set; } = mapComponent.CreatedOn;
    public string? LastUpdatedBy { get; set; } = mapComponent.LastUpdatedBy;
    public DateTime? LastUpdatedOn { get; set; } = mapComponent.LastUpdatedOn;
    public DateTime LatestUpdate => LastUpdatedOn ?? CreatedOn;
    public string? UpdateNotes { get; set; } = mapComponent.UpdateNotes;
    public string? UpdateNotesFormat { get; set; } = mapComponent.UpdateNotesFormat;

    public MapComponent ToDbObject()
    {
        return new()
        {
            ContentId = ContentId,
            ContentVersion = ContentVersion,
            CreatedBy = CreatedBy,
            CreatedOn = CreatedOn,
            InitialViewBoundsMaxLatitude = InitialViewBoundsMaxLatitude,
            InitialViewBoundsMaxLongitude = InitialViewBoundsMaxLongitude,
            InitialViewBoundsMinLatitude = InitialViewBoundsMinLatitude,
            InitialViewBoundsMinLongitude = InitialViewBoundsMinLongitude,
            LastUpdatedBy = LastUpdatedBy,
            LastUpdatedOn = LastUpdatedOn,
            Summary = Summary,
            Title = Title,
            UpdateNotes = UpdateNotes,
            UpdateNotesFormat = UpdateNotesFormat
        };
    }
}