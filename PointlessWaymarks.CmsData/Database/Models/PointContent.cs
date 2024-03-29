using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace PointlessWaymarks.CmsData.Database.Models;

public class PointContent : IUpdateNotes, IContentCommon
{
    public double? Elevation { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? MapIconName { get; set; }
    public string? MapLabel { get; set; }
    public string? MapMarkerColor { get; set; }
    public string? BodyContent { get; set; }
    public string? BodyContentFormat { get; set; }
    public required Guid ContentId { get; set; }
    public required DateTime ContentVersion { get; set; }
    public int Id { get; set; }
    public string? CreatedBy { get; set; }
    public required DateTime CreatedOn { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
    [NotMapped] public DateTime LatestUpdate => LastUpdatedOn ?? CreatedOn;
    public Guid? MainPicture { get; set; }
    public required DateTime FeedOn { get; set; }
    public bool IsDraft { get; set; }
    public bool ShowInMainSiteFeed { get; set; }
    public string? Tags { get; set; }
    public string? Folder { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Title { get; set; }
    public string? UpdateNotes { get; set; }
    public string? UpdateNotesFormat { get; set; }

    public static PointContent CreateInstance()
    {
        return NewContentModels.InitializePointContent(null);
    }

    /// <summary>
    ///     Returns a NTS Feature based on the Content data.
    /// </summary>
    /// <returns></returns>
    public IFeature FeatureFromPoint()
    {
        return new Feature(PointFromLatitudeLongitude(), new AttributesTable());
    }

    public static List<string> MapMarkerColorChoices()
    {
        return
        [
            "", "red", "darkred", "lightred", "orange", "beige", "green", "darkgreen", "lightgreen", "blue", "darkblue",
            "lightblue", "purple", "darkpurple", "pink", "cadetblue", "white", "gray", "lightgray", "black"
        ];
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
}