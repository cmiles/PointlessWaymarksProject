using CommunityToolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

public partial class GpxImportWaypoint : ObservableObject, IGpxImportListItem
{
    [ObservableProperty] private DateTime? _createdOn;
    [ObservableProperty] private Guid _displayId = Guid.NewGuid();
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private bool _replaceElevationOnImport;
    [ObservableProperty] private string _userContentName = string.Empty;
    [ObservableProperty] private string _userSummary = string.Empty;
    [ObservableProperty] private string _userMapLabel = string.Empty;
    [ObservableProperty] private GpxWaypoint _waypoint;

    public GpxImportWaypoint(GpxWaypoint waypoint)
    {
        _waypoint = waypoint;
    }

    public static async Task<GpxImportWaypoint> CreateInstance(GpxWaypoint toLoad, IProgress<string>? progress = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var toReturn = new GpxImportWaypoint(toLoad)
        {
            UserContentName = toLoad.Name.TrimNullToEmpty()
        };

        if (string.IsNullOrWhiteSpace(toLoad.Name))
        {
            toReturn.UserContentName = toLoad.TimestampUtc != null
                ? $"{toLoad.TimestampUtc:yyyy MMMM} Waypoint {toLoad.Latitude:F2}, {toLoad.Longitude:F2}"
                : $"Waypoint {toLoad.Latitude:F2}, {toLoad.Longitude:F2}";
        }

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