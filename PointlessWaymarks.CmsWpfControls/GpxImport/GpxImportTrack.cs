using NetTopologySuite.IO;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[NotifyPropertyChanged]
public partial class GpxImportTrack : IGpxImportListItem
{
    private GpxImportTrack(DistanceTools.LineStatsInImperial statistics, GpxTrack track,
        GpxTools.GpxTrackInformation trackInformation)
    {
        Statistics = statistics;
        Track = track;
        TrackInformation = trackInformation;
    }

    public string LineGeoJson { get; set; } = string.Empty;
    public DistanceTools.LineStatsInImperial Statistics { get; set; }
    public GpxTrack Track { get; set; }
    public GpxTools.GpxTrackInformation TrackInformation { get; set; }
    public DateTime? CreatedOn { get; set; }
    public Guid DisplayId { get; set; } = Guid.NewGuid();
    public bool MarkedForImport { get; set; }
    public bool ReplaceElevationOnImport { get; set; }
    public string UserContentName { get; set; } = string.Empty;
    public string UserSummary { get; set; } = string.Empty;

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