using CommunityToolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

public partial class GpxImportTrack : ObservableObject, IGpxImportListItem
{
    [ObservableProperty] private DateTime? _createdOn;
    [ObservableProperty] private Guid _displayId = Guid.NewGuid();
    [ObservableProperty] private string _lineGeoJson = string.Empty;
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private bool _replaceElevationOnImport;
    [ObservableProperty] private DistanceTools.LineStatsInImperial _statistics;
    [ObservableProperty] private GpxTrack _track;
    [ObservableProperty] private GpxTools.GpxTrackInformation _trackInformation;
    [ObservableProperty] private string _userContentName = string.Empty;
    [ObservableProperty] private string _userSummary = string.Empty;

    private GpxImportTrack(DistanceTools.LineStatsInImperial statistics, GpxTrack track, GpxTools.GpxTrackInformation trackInformation)
    {
        _statistics = statistics;
        _track = track;
        _trackInformation = trackInformation;
    }

    public static async Task<GpxImportTrack> CreateInstance(GpxTrack toLoad, IProgress<string>? progress = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var trackInformation = GpxTools.TrackInformationFromGpxTrack(toLoad);
        var statistics = DistanceTools.LineStatsInImperialFromCoordinateList(trackInformation.Track);

        var toReturn = new GpxImportTrack(statistics, toLoad, trackInformation);

        toReturn.LineGeoJson =
            await LineTools.GeoJsonWithLineStringFromCoordinateList(toReturn.TrackInformation.Track, false, progress);

        toReturn.CreatedOn = toLoad.Segments.FirstOrDefault()?.Waypoints.FirstOrDefault()?.TimestampUtc
            ?.ToLocalTime();

        toReturn.UserContentName = toLoad.Name.TrimNullToEmpty();
        if (string.IsNullOrWhiteSpace(toReturn.UserContentName))
        {
            if (toReturn.CreatedOn != null) toReturn.UserContentName = $"{toReturn.CreatedOn:yyyy MMMM} ";
            toReturn.UserContentName = $"{toReturn.UserContentName}Track";
            if (toReturn.TrackInformation.Track.Any())
                toReturn.UserContentName =
                    $"{toReturn.UserContentName} Starting {toReturn.TrackInformation.Track.First().Y:F2}, {toReturn.TrackInformation.Track.First().X:F2}";
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