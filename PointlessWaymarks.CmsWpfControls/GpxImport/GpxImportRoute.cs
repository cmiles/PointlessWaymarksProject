using CommunityToolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportRoute : IGpxImportListItem
{
    [ObservableProperty] private DateTime? _createdOn;
    [ObservableProperty] private Guid _displayId;
    [ObservableProperty] private string _lineGeoJson;
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private bool _replaceElevationOnImport;
    [ObservableProperty] private GpxRoute _route;
    [ObservableProperty] private GpxTools.GpxRouteInformation _routeInformation;
    [ObservableProperty] private SpatialHelpers.LineStatsInImperial _statistics;
    [ObservableProperty] private string _userContentName;
    [ObservableProperty] private string _userSummary;

    public async Task Load(GpxRoute toLoad, IProgress<string> progress = null)
    {
        DisplayId = Guid.NewGuid();
        Route = toLoad;
        RouteInformation = GpxTools.RouteInformationFromGpxRoute(toLoad);
        LineGeoJson =
            await LineTools.GeoJsonWithLineStringFromCoordinateList(RouteInformation.Track, false, progress);
        Statistics = SpatialHelpers.LineStatsInImperialFromCoordinateList(RouteInformation.Track);
        CreatedOn = toLoad.Waypoints?.FirstOrDefault()?.TimestampUtc?.ToLocalTime();

        UserContentName = toLoad.Name.TrimNullToEmpty();
        if (string.IsNullOrWhiteSpace(UserContentName))
        {
            if (CreatedOn != null) UserContentName = $"{CreatedOn:yyyy MMMM} ";
            UserContentName = $"{UserContentName}Track";
            if (RouteInformation.Track.Any())
                UserContentName =
                    $"{UserContentName} Starting {RouteInformation.Track.First().Y:F2}, {RouteInformation.Track.First().X:F2}";
        }

        var userSummary = string.Empty;

        if (!string.IsNullOrWhiteSpace(toLoad.Comment)) userSummary = toLoad.Comment.Trim();

        if (!string.IsNullOrWhiteSpace(toLoad.Description))
            if (!string.IsNullOrWhiteSpace(toLoad.Comment) &&
                !toLoad.Comment.Equals(toLoad.Description, StringComparison.OrdinalIgnoreCase))
                userSummary += $" {toLoad.Description.Trim()}";

        UserSummary = userSummary;
    }
}