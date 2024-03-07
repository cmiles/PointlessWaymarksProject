namespace PointlessWaymarks.CmsData.Database.Models;

public class MapComponentDto
    : IContentId, ICreatedAndLastUpdateOnAndBy, IUpdateNotes
{
    public MapComponentDto()
    {
    }

    public MapComponentDto(MapComponent mapComponent, List<MapElement> elementList)
    {
        Elements = elementList;
        InitialViewBoundsMaxLatitude = mapComponent.InitialViewBoundsMaxLatitude;
        InitialViewBoundsMaxLongitude = mapComponent.InitialViewBoundsMaxLongitude;
        InitialViewBoundsMinLatitude = mapComponent.InitialViewBoundsMinLatitude;
        InitialViewBoundsMinLongitude = mapComponent.InitialViewBoundsMinLongitude;
        Summary = mapComponent.Summary;
        Title = mapComponent.Title;
        ContentId = mapComponent.ContentId;
        ContentVersion = mapComponent.ContentVersion;
        Id = mapComponent.Id;
        CreatedBy = mapComponent.CreatedBy;
        CreatedOn = mapComponent.CreatedOn;
        LastUpdatedBy = mapComponent.LastUpdatedBy;
        LastUpdatedOn = mapComponent.LastUpdatedOn;
        UpdateNotes = mapComponent.UpdateNotes;
        UpdateNotesFormat = mapComponent.UpdateNotesFormat;
    }

    public List<MapElement> Elements { get; set; } = new();
    public double InitialViewBoundsMaxLatitude { get; set; }
    public double InitialViewBoundsMaxLongitude { get; set; }
    public double InitialViewBoundsMinLatitude { get; set; }
    public double InitialViewBoundsMinLongitude { get; set; }
    public string? Summary { get; set; }
    public string? Title { get; set; }
    public Guid ContentId { get; set; }
    public DateTime ContentVersion { get; set; }
    public int Id { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
    public DateTime LatestUpdate => LastUpdatedOn ?? CreatedOn;
    public string? UpdateNotes { get; set; }
    public string? UpdateNotesFormat { get; set; }

    public MapComponent ToDbObject()
    {
        return new MapComponent
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