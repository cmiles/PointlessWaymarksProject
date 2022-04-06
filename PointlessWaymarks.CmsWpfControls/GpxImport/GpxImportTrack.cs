using Microsoft.Toolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData.Spatial;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportTrack : IGpxImportListItem
{
    [ObservableProperty] private DateTime? _createdOn;
    [ObservableProperty] private Guid _displayId;
    [ObservableProperty] private string _lineGeoJson;
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private bool _replaceElevationOnImport;
    [ObservableProperty] private SpatialHelpers.LineStatsInImperial _statistics;
    [ObservableProperty] private GpxTrack _track;
    [ObservableProperty] private SpatialHelpers.GpxTrackInformation _trackInformation;
    [ObservableProperty] private string _userContentName;
    [ObservableProperty] private string _userSummary;

    public async Task Load(GpxTrack toLoad, IProgress<string> progress = null)
    {
        DisplayId = Guid.NewGuid();
        Track = toLoad;
        TrackInformation = SpatialHelpers.TrackInformationFromGpxTrack(toLoad);
        LineGeoJson =
            await SpatialHelpers.GeoJsonWithLineStringFromCoordinateList(TrackInformation.Track, false, progress);
        Statistics = SpatialHelpers.LineStatsInImperialFromCoordinateList(TrackInformation.Track);
        UserContentName = toLoad.Name ?? string.Empty;
        CreatedOn = toLoad.Segments.FirstOrDefault()?.Waypoints.FirstOrDefault()?.TimestampUtc
            ?.ToLocalTime();

        var userSummary = string.Empty;

        if (!string.IsNullOrWhiteSpace(toLoad.Comment)) userSummary = toLoad.Comment.Trim();

        if (!string.IsNullOrWhiteSpace(toLoad.Description))
            if (!string.IsNullOrWhiteSpace(toLoad.Comment) &&
                !toLoad.Comment.Equals(toLoad.Description, StringComparison.OrdinalIgnoreCase))
                userSummary += $" {toLoad.Description.Trim()}";

        UserSummary = userSummary;
    }
}