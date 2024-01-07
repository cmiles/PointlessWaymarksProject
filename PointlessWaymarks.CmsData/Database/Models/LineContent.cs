using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData.Database.Models;

[JsonAlphabeticalPropertyOrdering]
public class LineContent : IUpdateNotes, IContentCommon
{
    public string? BodyContent { get; set; }
    public string? BodyContentFormat { get; set; }
    public double ClimbElevation { get; set; }
    public required Guid ContentId { get; set; }
    public required DateTime ContentVersion { get; set; }
    public string? CreatedBy { get; set; }
    public required DateTime CreatedOn { get; set; }
    public double DescentElevation { get; set; }
    public required DateTime FeedOn { get; set; }
    public string? Folder { get; set; }

    [JsonPropertyOrder(0)] public int Id { get; set; }

    public double InitialViewBoundsMaxLatitude { get; set; }
    public double InitialViewBoundsMaxLongitude { get; set; }
    public double InitialViewBoundsMinLatitude { get; set; }
    public double InitialViewBoundsMinLongitude { get; set; }
    public bool IsDraft { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
    [NotMapped] [JsonIgnore] public DateTime LatestUpdate => LastUpdatedOn ?? CreatedOn;
    public string? Line { get; set; }
    public double LineDistance { get; set; }
    public Guid? MainPicture { get; set; }
    public double MaximumElevation { get; set; }
    public double MinimumElevation { get; set; }
    public bool PublicDownloadLink { get; set; }
    public DateTime? RecordingEndedOn { get; set; }
    public DateTime? RecordingEndedOnUtc { get; set; }
    public DateTime? RecordingStartedOn { get; set; }
    public DateTime? RecordingStartedOnUtc { get; set; }
    public bool ShowInMainSiteFeed { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Tags { get; set; }
    public string? Title { get; set; }
    public string? UpdateNotes { get; set; }
    public string? UpdateNotesFormat { get; set; }

    public static LineContent CreateInstance()
    {
        return NewContentModels.InitializeLineContent(null);
    }

    /// <summary>
    ///     Transforms the Line (stored as GeoJson) to an NTS Feature. The assumption is that
    ///     the Line is both valid Json and conforms to the conventions of this program - invalid
    ///     data will Throw and a null or empty line will return null.
    /// </summary>
    /// <returns></returns>
    public IFeature? FeatureFromGeoJsonLine()
    {
        return FeatureFromGeoJsonLine(Line);
    }

    /// <summary>
    ///     Transforms the Line (stored as GeoJson) to an NTS Feature. The assumption is that
    ///     the Line is both valid Json and conforms to the conventions of this program - invalid
    ///     data will Throw and a null or empty line will return null.
    /// </summary>
    /// <returns></returns>
    public static IFeature? FeatureFromGeoJsonLine(string? lineString)
    {
        if (string.IsNullOrWhiteSpace(lineString)) return null;

        var featureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(lineString);

        return featureCollection.FirstOrDefault();
    }

    /// <summary>
    ///     Transforms the Line (stored as GeoJson) to an NTS LineString. The assumption is that
    ///     the Line is both valid Json and conforms to the conventions of this program - invalid
    ///     data will Throw and a null or empty line will return null.
    /// </summary>
    /// <returns></returns>
    public LineString? LineStringFromGeoJsonLine()
    {
        if (string.IsNullOrWhiteSpace(Line)) return null;

        var featureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(Line);

        return featureCollection.FirstOrDefault()?.Geometry as LineString;
    }
}