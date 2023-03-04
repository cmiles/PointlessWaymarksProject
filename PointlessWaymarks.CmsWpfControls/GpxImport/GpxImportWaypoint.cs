using CommunityToolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

public partial class GpxImportWaypoint : ObservableObject, IGpxImportListItem
{
    [ObservableProperty] private DateTime? _createdOn;
    [ObservableProperty] private Guid _displayId;
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private bool _replaceElevationOnImport;
    [ObservableProperty] private string _userContentName;
    [ObservableProperty] private string _userSummary;
    [ObservableProperty] private string _userMapLabel;
    [ObservableProperty] private GpxWaypoint _waypoint;


    public async Task Load(GpxWaypoint toLoad, IProgress<string>? progress = null)
    {
        DisplayId = Guid.NewGuid();
        Waypoint = toLoad;

        UserContentName = toLoad.Name.TrimNullToEmpty();
        if (string.IsNullOrWhiteSpace(toLoad.Name))
        {
            UserContentName = toLoad.TimestampUtc != null
                ? $"{toLoad.TimestampUtc:yyyy MMMM} Waypoint {toLoad.Latitude:F2}, {toLoad.Longitude:F2}"
                : $"Waypoint {toLoad.Latitude:F2}, {toLoad.Longitude:F2}";
        }

        CreatedOn = toLoad.TimestampUtc?.ToLocalTime();

        var userSummary = string.Empty;

        if (!string.IsNullOrWhiteSpace(toLoad.Comment)) userSummary = toLoad.Comment.Trim();

        if (!string.IsNullOrWhiteSpace(toLoad.Description))
            if (!string.IsNullOrWhiteSpace(toLoad.Comment) &&
                !toLoad.Comment.Equals(toLoad.Description, StringComparison.OrdinalIgnoreCase))
                userSummary += $" {toLoad.Description.Trim()}";

        UserSummary = userSummary;

        UserMapLabel = string.Empty;
    }
}