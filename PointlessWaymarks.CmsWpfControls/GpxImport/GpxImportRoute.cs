using Microsoft.Toolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData.Spatial;

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
    [ObservableProperty] private SpatialHelpers.GpxRouteInformation _routeInformation;
    [ObservableProperty] private SpatialHelpers.LineStatsInImperial _statistics;
    [ObservableProperty] private string _userContentName;
    [ObservableProperty] private string _userSummary;

    public async Task Load(GpxRoute toLoad, IProgress<string> progress = null)
    {
        DisplayId = Guid.NewGuid();
        Route = toLoad;
        RouteInformation = SpatialHelpers.RouteInformationFromGpxRoute(toLoad);
        LineGeoJson =
            await SpatialHelpers.GeoJsonWithLineStringFromCoordinateList(RouteInformation.Track, false, progress);
        Statistics = SpatialHelpers.LineStatsInImperialFromCoordinateList(RouteInformation.Track);
        UserContentName = toLoad.Name ?? string.Empty;
        CreatedOn = toLoad.Waypoints?.FirstOrDefault()?.TimestampUtc?.ToLocalTime();

        var userSummary = string.Empty;

        if (!string.IsNullOrWhiteSpace(toLoad.Comment)) userSummary = toLoad.Comment.Trim();

        if (!string.IsNullOrWhiteSpace(toLoad.Description))
            if (!string.IsNullOrWhiteSpace(toLoad.Comment) &&
                !toLoad.Comment.Equals(toLoad.Description, StringComparison.OrdinalIgnoreCase))
                userSummary += $" {toLoad.Description.Trim()}";

        UserSummary = userSummary;
    }
}