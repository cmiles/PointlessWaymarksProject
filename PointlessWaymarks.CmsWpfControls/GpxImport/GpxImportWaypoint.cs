using NetTopologySuite.IO;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[NotifyPropertyChanged]
public partial class GpxImportWaypoint : IGpxImportListItem
{
    public GpxImportWaypoint(GpxWaypoint waypoint)
    {
        Waypoint = waypoint;
    }

    public string UserMapLabel { get; set; } = string.Empty;
    public GpxWaypoint Waypoint { get; set; }
    public DateTime? CreatedOn { get; set; }
    public Guid DisplayId { get; set; } = Guid.NewGuid();
    public bool MarkedForImport { get; set; }
    public bool ReplaceElevationOnImport { get; set; }
    public string UserContentName { get; set; } = string.Empty;
    public string UserSummary { get; set; } = string.Empty;

    public static async Task<GpxImportWaypoint> CreateInstance(GpxWaypoint toLoad, IProgress<string>? progress = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var toReturn = new GpxImportWaypoint(toLoad)
        {
            UserContentName = toLoad.Name.TrimNullToEmpty()
        };

        if (string.IsNullOrWhiteSpace(toLoad.Name))
            toReturn.UserContentName = toLoad.TimestampUtc != null
                ? $"{toLoad.TimestampUtc:yyyy MMMM} Waypoint {toLoad.Latitude:F2}, {toLoad.Longitude:F2}"
                : $"Waypoint {toLoad.Latitude:F2}, {toLoad.Longitude:F2}";

        toReturn.CreatedOn = toLoad.TimestampUtc?.ToLocalTime();

        var userSummary = string.Empty;

        if (!string.IsNullOrWhiteSpace(toLoad.Comment)) userSummary = toLoad.Comment.Trim();

        if (!string.IsNullOrWhiteSpace(toLoad.Description))
            if (!string.IsNullOrWhiteSpace(toLoad.Comment) &&
                !toLoad.Comment.Equals(toLoad.Description, StringComparison.OrdinalIgnoreCase))
                userSummary += $" {toLoad.Description.Trim()}";

        toReturn.UserSummary = userSummary;

        toReturn.UserMapLabel = string.Empty;

        return toReturn;
    }
}