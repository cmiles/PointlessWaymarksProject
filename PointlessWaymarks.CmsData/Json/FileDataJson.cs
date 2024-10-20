using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData.Json;

public record FileContentOnDiskData(string ContentType, FileContent Content);

public record GeoJsonContentOnDiskData(string ContentType, GeoJsonContent Content);

public record ImageContentOnDiskData(
    string ContentType,
    ImageContent Content,
    string? SmallPictureUrl,
    string? DisplayPictureUrl);

public record LineContentOnDiskData(
    string ContentType,
    LineContent Content,
    List<LineElevationChartDataPoint> ElevationPlotData,
    SpatialContentReferences MapElements);

public record MapComponentOnDiskData(
    string ContentType,
    MapComponentDto Content,
    SpatialContentReferences MapElements,
    List<Guid> ShowDetailsElements);

public record SpatialContentReferences(
    List<SpatialContentReference> FileContentIds,
    List<SpatialContentReference> GeoJsonContentIds,
    List<SpatialContentReference> ImageContentIds,
    List<SpatialContentReference> LineContentIds,
    List<SpatialContentReference> PhotoContentIds,
    List<SpatialContentReference> PointContentIds,
    List<SpatialContentReference> PostContentIds,
    List<SpatialContentReference> VideoContentIds
);

public record SpatialContentReference
{
    public Guid ContentId { get; set; }
    public string LinkTo { get; set; } = string.Empty;

    public static List<SpatialContentReference> FromListOfContentIds(List<Guid> contentIds)
    {
        return contentIds.Distinct().Select(x => new SpatialContentReference { ContentId = x }).ToList();
    }
}

public record NoteContentOnDiskData(string ContentType, NoteContent Content);

public record PhotoContentOnDiskData(
    string ContentType,
    PhotoContent Content,
    string? SmallPictureUrl,
    string? DisplayPictureUrl);

public record PointContentOnDiskData(string ContentType, PointContentDto Content);

public record PostContentOnDiskData(string ContentType, PostContent Content);

public record SnippetOnDiskData(string ContentType, Snippet Content);

public record TrailContentOnDiskData(string ContentType, TrailContent Content);

public record VideoContentOnDiskData(string ContentType, VideoContent Content);