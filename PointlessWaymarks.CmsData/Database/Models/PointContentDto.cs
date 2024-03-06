using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CmsData.Database.Models;

public class PointContentDto : IUpdateNotes, IContentCommon
{
    public string? BodyContent { get; set; }
    public string? BodyContentFormat { get; set; }
    public Guid ContentId { get; set; }
    public DateTime ContentVersion { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public double? Elevation { get; set; }
    public DateTime FeedOn { get; set; }
    public string? Folder { get; set; }
    public int Id { get; set; }
    public bool IsDraft { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
    public DateTime LatestUpdate => LastUpdatedOn ?? CreatedOn;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Guid? MainPicture { get; set; }
    public string? MapIconName { get; set; }
    public string? MapLabel { get; set; }
    public string? MapMarkerColor { get; set; }
    public List<PointDetail> PointDetails { get; set; } = new();
    public bool ShowInMainSiteFeed { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Tags { get; set; }
    public string? Title { get; set; }
    public string? UpdateNotes { get; set; }
    public string? UpdateNotesFormat { get; set; }

    /// <summary>
    ///     Returns a NTS Feature based on the Content data.
    /// </summary>
    /// <returns></returns>
    public IFeature FeatureFromPoint()
    {
        return new Feature(PointFromLatitudeLongitude(), new AttributesTable());
    }


    /// <summary>
    ///     Returns either a Point or a PointZ from the Contents Values
    /// </summary>
    /// <returns></returns>
    public Point PointFromLatitudeLongitude()
    {
        if (Elevation is null) return new Point(Longitude, Latitude);
        return new Point(Longitude, Latitude, Elevation.Value);
    }

    public PointContent ToDbObject()
    {
        return new()
        {
            BodyContent = BodyContent,
            BodyContentFormat = BodyContentFormat,
            ContentId = ContentId,
            ContentVersion = ContentVersion,
            CreatedBy = CreatedBy,
            CreatedOn = CreatedOn,
            Elevation = Elevation,
            FeedOn = FeedOn,
            Folder = Folder,
            IsDraft = IsDraft,
            LastUpdatedBy = LastUpdatedBy,
            LastUpdatedOn = LastUpdatedOn,
            Latitude = Latitude,
            Longitude = Longitude,
            MainPicture = MainPicture,
            MapLabel = MapLabel,
            ShowInMainSiteFeed = ShowInMainSiteFeed,
            Slug = Slug,
            Summary = Summary,
            Tags = Tags,
            Title = Title,
            UpdateNotes = UpdateNotes,
            UpdateNotesFormat = UpdateNotesFormat
        };
    }

    public HistoricPointContent ToHistoricDbObject()
    {
        return new()
        {
            BodyContent = BodyContent,
            BodyContentFormat = BodyContentFormat,
            ContentId = ContentId,
            ContentVersion = ContentVersion,
            CreatedBy = CreatedBy,
            CreatedOn = CreatedOn,
            Elevation = Elevation,
            FeedOn = FeedOn,
            Folder = Folder,
            IsDraft = IsDraft,
            LastUpdatedBy = string.IsNullOrWhiteSpace(LastUpdatedBy) ? "Historic Entry Archivist" : LastUpdatedBy,
            LastUpdatedOn = LastUpdatedOn ?? DateTime.Now,
            Latitude = Latitude,
            Longitude = Longitude,
            MainPicture = MainPicture,
            MapLabel = MapLabel,
            ShowInMainSiteFeed = ShowInMainSiteFeed,
            Slug = Slug,
            Summary = Summary,
            Tags = Tags,
            Title = Title,
            UpdateNotes = UpdateNotes,
            UpdateNotesFormat = UpdateNotesFormat,
            PointDetails = System.Text.Json.JsonSerializer.Serialize(PointDetails, JsonTools.WriteIndentedOptions)
        };
    }
}