using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace PointlessWaymarks.CmsData.Database.Models;

public class PhotoContent : IUpdateNotes, IContentCommon
{
    public string? AltText { get; set; }
    public string? Aperture { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public double? Elevation { get; set; }
    public string? FocalLength { get; set; }
    public int? Iso { get; set; }
    public double? Latitude { get; set; }
    public string? Lens { get; set; }
    public string? License { get; set; }
    public double? Longitude { get; set; }
    public string? OriginalFileName { get; set; }
    public string? PhotoCreatedBy { get; set; }
    public DateTime PhotoCreatedOn { get; set; }
    public bool ShowPhotoSizes { get; set; }
    public string? ShutterSpeed { get; set; }
    public string? BodyContent { get; set; }
    public string? BodyContentFormat { get; set; }
    public Guid ContentId { get; set; }
    public DateTime ContentVersion { get; set; }
    public int Id { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
    [NotMapped] public DateTime LatestUpdate => LastUpdatedOn ?? CreatedOn;
    public Guid? MainPicture { get; set; }
    public DateTime FeedOn { get; set; }
    public bool IsDraft { get; set; }
    public bool ShowInMainSiteFeed { get; set; }
    public string? Tags { get; set; }
    public string? Folder { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Title { get; set; }
    public string? UpdateNotes { get; set; }
    public string? UpdateNotesFormat { get; set; }

    /// <summary>
    ///     Returns a NTS Feature based on the Content data.
    /// </summary>
    /// <returns></returns>
    public IFeature? FeatureFromPoint()
    {
        if (Longitude is null || Latitude is null) return null;
        return new Feature(PointFromLatitudeLongitude(), new AttributesTable());
    }

    /// <summary>
    ///     Returns either a Point or a PointZ from the Contents Values
    /// </summary>
    /// <returns></returns>
    public Point? PointFromLatitudeLongitude()
    {
        if (Longitude is null || Latitude is null) return null;
        if (Elevation is null) return new Point(Longitude.Value, Latitude.Value);
        return new Point(Longitude.Value, Latitude.Value, Elevation.Value);
    }
}