using NetTopologySuite.IO;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[NotifyPropertyChanged]
public partial class GpxImportRoute : IGpxImportListItem
{
    private GpxImportRoute(GpxRoute route, GpxTools.GpxRouteInformation routeInformation,
        DistanceTools.LineStatsInImperial statistics)
    {
        Route = route;
        RouteInformation = routeInformation;
        Statistics = statistics;
    }

    public string LineGeoJson { get; set; } = string.Empty;
    public GpxRoute Route { get; set; }
    public GpxTools.GpxRouteInformation RouteInformation { get; set; }
    public DistanceTools.LineStatsInImperial Statistics { get; set; }
    public DateTime? CreatedOn { get; set; }
    public Guid DisplayId { get; set; } = Guid.NewGuid();
    public bool MarkedForImport { get; set; }
    public bool ReplaceElevationOnImport { get; set; }
    public string UserContentName { get; set; } = string.Empty;
    public string UserSummary { get; set; } = string.Empty;

    public static async Task<GpxImportRoute> CreateInstance(GpxRoute toLoad, IProgress<string>? progress = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var routeInformation = GpxTools.RouteInformationFromGpxRoute(toLoad);
        var statistics = DistanceTools.LineStatsInImperialFromCoordinateList(routeInformation.Track);

        var toReturn = new GpxImportRoute(toLoad, routeInformation, statistics)
        {
            Route = toLoad
        };

        toReturn.LineGeoJson =
            await LineTools.GeoJsonWithLineStringFromCoordinateList(toReturn.RouteInformation.Track, false, progress);
        toReturn.CreatedOn = toLoad.Waypoints?.FirstOrDefault()?.TimestampUtc?.ToLocalTime();

        toReturn.UserContentName = toLoad.Name.TrimNullToEmpty();
        if (string.IsNullOrWhiteSpace(toReturn.UserContentName))
        {
            if (toReturn.CreatedOn != null) toReturn.UserContentName = $"{toReturn.CreatedOn:yyyy MMMM} ";
            toReturn.UserContentName = $"{toReturn.UserContentName}Track";
            if (toReturn.RouteInformation.Track.Any())
                toReturn.UserContentName =
                    $"{toReturn.UserContentName} Starting {toReturn.RouteInformation.Track.First().Y:F2}, {toReturn.RouteInformation.Track.First().X:F2}";
        }

        var userSummary = string.Empty;

        if (!string.IsNullOrWhiteSpace(toLoad.Comment)) userSummary = toLoad.Comment.Trim();

        if (!string.IsNullOrWhiteSpace(toLoad.Description))
            if (!string.IsNullOrWhiteSpace(toLoad.Comment) &&
                !toLoad.Comment.Equals(toLoad.Description, StringComparison.OrdinalIgnoreCase))
                userSummary += $" {toLoad.Description.Trim()}";

        toReturn.UserSummary = userSummary;

        return toReturn;
    }
}