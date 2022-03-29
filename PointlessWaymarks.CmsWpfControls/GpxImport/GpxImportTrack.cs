using Microsoft.Toolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData.Spatial;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportTrack : IGpxImportListItem
{
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private GpxTrack _track;
    [ObservableProperty] private SpatialHelpers.GpxTrackInformation _trackInformation;
    [ObservableProperty] private SpatialHelpers.LineStatsInImperial _statistics;
    [ObservableProperty] private string _lineGeoJson;
    [ObservableProperty] private bool _replaceElevationOnImport;

    public async Task LoadTrack(GpxTrack toLoad, IProgress<string> progress = null)
    {
        Track = toLoad;
        TrackInformation = SpatialHelpers.TrackInformationFromGpxTrack(toLoad);
        LineGeoJson =
            await SpatialHelpers.GeoJsonWithLineStringFromCoordinateList(TrackInformation.Track, false, progress);
        Statistics = SpatialHelpers.LineStatsInImperialFromCoordinateList(TrackInformation.Track);
    }
}