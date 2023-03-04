using CommunityToolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

public partial class GpxImportTrack : ObservableObject, IGpxImportListItem
{
    [ObservableProperty] private DateTime? _createdOn;
    [ObservableProperty] private Guid _displayId;
    [ObservableProperty] private string _lineGeoJson;
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private bool _replaceElevationOnImport;
    [ObservableProperty] private DistanceTools.LineStatsInImperial _statistics;
    [ObservableProperty] private GpxTrack _track;
    [ObservableProperty] private GpxTools.GpxTrackInformation _trackInformation;
    [ObservableProperty] private string _userContentName;
    [ObservableProperty] private string _userSummary;

    public async Task Load(GpxTrack toLoad, IProgress<string>? progress = null)
    {
        DisplayId = Guid.NewGuid();
        Track = toLoad;
        TrackInformation = GpxTools.TrackInformationFromGpxTrack(toLoad);
        LineGeoJson =
            await LineTools.GeoJsonWithLineStringFromCoordinateList(TrackInformation.Track, false, progress);
        Statistics = DistanceTools.LineStatsInImperialFromCoordinateList(TrackInformation.Track);

        CreatedOn = toLoad.Segments.FirstOrDefault()?.Waypoints.FirstOrDefault()?.TimestampUtc
            ?.ToLocalTime();

        UserContentName = toLoad.Name.TrimNullToEmpty();
        if (string.IsNullOrWhiteSpace(UserContentName))
        {
            if (CreatedOn != null) UserContentName = $"{CreatedOn:yyyy MMMM} ";
            UserContentName = $"{UserContentName}Track";
            if (TrackInformation.Track.Any())
                UserContentName =
                    $"{UserContentName} Starting {TrackInformation.Track.First().Y:F2}, {TrackInformation.Track.First().X:F2}";
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