using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Features;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData.Database.Models;

public class GeoJsonContent : IUpdateNotes, IContentCommon
{
    public string? GeoJson { get; set; }
    public double InitialViewBoundsMaxLatitude { get; set; }
    public double InitialViewBoundsMaxLongitude { get; set; }
    public double InitialViewBoundsMinLatitude { get; set; }
    public double InitialViewBoundsMinLongitude { get; set; }
    public bool PublicDownloadLink { get; set; }
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
    public bool ShowInSearch { get; set; } = true;
    public string? Tags { get; set; }
    public string? Folder { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Title { get; set; }
    public string? UpdateNotes { get; set; }
    public string? UpdateNotesFormat { get; set; }

    public static GeoJsonContent CreateInstance()
    {
        return NewContentModels.InitializeGeoJsonContent(null);
    }

    /// <summary>
    ///     Returns the GeoJson as a List of NTS IFeature
    /// </summary>
    /// <returns></returns>
    public static List<IFeature> FeaturesFromGeoJson(string? geoJson)
    {
        var returnList = new List<IFeature>();

        if (string.IsNullOrWhiteSpace(geoJson)) return returnList;

        return GeoJsonTools.DeserializeStringToFeatureCollection(geoJson).ToList();
    }

    /// <summary>
    ///     Returns the GeoJson as a List of NTS IFeature
    /// </summary>
    /// <returns></returns>
    public List<IFeature> FeaturesFromGeoJson()
    {
        return FeaturesFromGeoJson(GeoJson);
    }
}