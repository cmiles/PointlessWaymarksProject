using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData.Database.Models;

public class LineContent : IUpdateNotes, IContentCommon
{
    public double ClimbElevation { get; set; }
    public double DescentElevation { get; set; }
    public double InitialViewBoundsMaxLatitude { get; set; }
    public double InitialViewBoundsMaxLongitude { get; set; }
    public double InitialViewBoundsMinLatitude { get; set; }
    public double InitialViewBoundsMinLongitude { get; set; }
    public string? Line { get; set; }
    public double LineDistance { get; set; }
    public double MaximumElevation { get; set; }
    public double MinimumElevation { get; set; }
    public DateTime? RecordingEndedOn { get; set; }
    public DateTime? RecordingStartedOn { get; set; }
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