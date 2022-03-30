using Microsoft.Toolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData.Spatial;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportTrack : IGpxImportListItem
{
    [ObservableProperty] private Guid _displayId;
    [ObservableProperty] private string _lineGeoJson;
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private bool _replaceElevationOnImport;
    [ObservableProperty] private SpatialHelpers.LineStatsInImperial _statistics;
    [ObservableProperty] private GpxTrack _track;
    [ObservableProperty] private SpatialHelpers.GpxTrackInformation _trackInformation;

    public async Task Load(GpxTrack toLoad, IProgress<string> progress = null)
    {
        DisplayId = Guid.NewGuid();
        Track = toLoad;
        TrackInformation = SpatialHelpers.TrackInformationFromGpxTrack(toLoad);
        LineGeoJson =
            await SpatialHelpers.GeoJsonWithLineStringFromCoordinateList(TrackInformation.Track, false, progress);
        Statistics = SpatialHelpers.LineStatsInImperialFromCoordinateList(TrackInformation.Track);
    }
}