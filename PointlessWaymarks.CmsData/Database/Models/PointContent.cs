using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CommonTools;

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
            "", "red", "darkred", "lightred", "orange", "green", "darkgreen", "lightgreen", "blue", "darkblue",
            "lightblue", "cadetblue", "purple", "darkpurple", "pink", "beige", "black", "gray", "lightgray", "white"
        ];
    }

    public static Dictionary<string, string> MapMarkerColorChoicesDictionary()
    {
        var toReturn = new Dictionary<string, string> { { "", string.Empty } };

        return toReturn.Concat(MapMarkerColorDictionary()).ToDictionary(x => x.Key, x => x.Value);
    }

    public static Dictionary<string, string> MapMarkerColorDictionary()
    {
        return new Dictionary<string, string>
        {
            { "red", "#d63e2a" },
            { "darkred", "#a13336" },
            { "lightred", "#ff8e7f" },
            { "orange", "#f69730" },
            { "beige", "#ffcb92" },
            { "green", "#72b026" },
            { "darkgreen", "#728224" },
            { "lightgreen", "#bbf970" },
            { "blue", "#38aadd" },
            { "darkblue", "#00649f" },
            { "lightblue", "#8adaff" },
            { "purple", "#d152b8" },
            { "darkpurple", "#5b396b" },
            { "pink", "#ff91ea" },
            { "cadetblue", "#436978" },
            { "white", "#fbfbfb" },
            { "gray", "#575757" },
            { "lightgray", "#a3a3a3" },
            { "black", "#303030" }
        };
    }

    /// <summary>
    ///     Returns either a Point or a PointZ from the Contents Values
    /// </summary>
    /// <returns></returns>
    public Point PointFromLatitudeLongitude()
    {
        if (Elevation is null) return new Point(Longitude, Latitude);
        return new Point(Longitude, Latitude, Elevation.FeetToMeters());
    }
}