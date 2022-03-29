using Microsoft.Toolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData.Spatial;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportRoute : IGpxImportListItem
{
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private GpxRoute _route;
    [ObservableProperty] private SpatialHelpers.GpxRouteInformation _routeInformation;
    [ObservableProperty] private SpatialHelpers.LineStatsInImperial _statistics;
    [ObservableProperty] private string _lineGeoJson;
    [ObservableProperty] private bool _replaceElevationOnImport;

    public async Task LoadTrack(GpxRoute toLoad, IProgress<string> progress = null)
    {
        Route = toLoad;
        RouteInformation = SpatialHelpers.RouteInformationFromGpxRoute(toLoad);
        LineGeoJson =
            await SpatialHelpers.GeoJsonWithLineStringFromCoordinateList(RouteInformation.Track, false, progress);
        Statistics = SpatialHelpers.LineStatsInImperialFromCoordinateList(RouteInformation.Track);
    }
}