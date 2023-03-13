using CommunityToolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

public partial class GpxImportRoute : ObservableObject, IGpxImportListItem
{
    [ObservableProperty] private DateTime? _createdOn;
    [ObservableProperty] private Guid _displayId = Guid.NewGuid();
    [ObservableProperty] private string _lineGeoJson = string.Empty;
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private bool _replaceElevationOnImport;
    [ObservableProperty] private GpxRoute _route;
    [ObservableProperty] private GpxTools.GpxRouteInformation _routeInformation;
    [ObservableProperty] private DistanceTools.LineStatsInImperial _statistics;
    [ObservableProperty] private string _userContentName = string.Empty;
    [ObservableProperty] private string _userSummary = string.Empty;

    private GpxImportRoute(GpxRoute route, GpxTools.GpxRouteInformation routeInformation, DistanceTools.LineStatsInImperial statistics)
    {
        _route = route;
        _routeInformation = routeInformation;
        _statistics = statistics;
    }

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