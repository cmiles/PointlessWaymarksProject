using NetTopologySuite.Features;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData.Json;



public record FileContentOnDiskData(string ContentType, FileContent Content);


public record GeoJsonContentOnDiskData(string ContentType, GeoJsonContent Content);

public record ImageContentOnDiskData(string ContentType, ImageContent Content, string? SmallPictureUrl, string? DisplayPictureUrl);

public record LineContentOnDiskData(string ContentType, LineContent Content, List<LineElevationChartDataPoint> ElevationPlotData, SpatialContentIdReferences MapElements);

public record MapComponentOnDiskData(string ContentType, MapComponentDto Content, SpatialContentIdReferences MapElements, List<Guid> ShowDetailsElements);

public record SpatialContentIdReferences(
    List<Guid> PointContentIds,
    List<Guid> LineContentIds,
    List<Guid> GeoJsonContentIds,
    List<Guid> PhotoContentIds);

public record NoteContentOnDiskData(string ContentType, NoteContent Content);

public record PhotoContentOnDiskData(string ContentType, PhotoContent Content, string? SmallPictureUrl, string? DisplayPictureUrl);

public record PointContentOnDiskData(string ContentType, PointContentDto Content);

public record PostContentOnDiskData(string ContentType, PostContent Content);

public record VideoContentOnDiskData(string ContentType, VideoContent Content);