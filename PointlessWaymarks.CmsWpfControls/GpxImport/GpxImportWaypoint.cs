using Microsoft.Toolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportWaypoint : IGpxImportListItem
{
    [ObservableProperty] private DateTime? _createdOn;
    [ObservableProperty] private Guid _displayId;
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private bool _replaceElevationOnImport;
    [ObservableProperty] private string _userContentName;
    [ObservableProperty] private string _userSummary;
    [ObservableProperty] private GpxWaypoint _waypoint;


    public async Task Load(GpxWaypoint toLoad, IProgress<string> progress = null)
    {
        DisplayId = Guid.NewGuid();
        Waypoint = toLoad;
        UserContentName = toLoad.Name ?? string.Empty;
        CreatedOn = toLoad.TimestampUtc?.ToLocalTime();

        var userSummary = string.Empty;

        if (!string.IsNullOrWhiteSpace(toLoad.Comment)) userSummary = toLoad.Comment.Trim();

        if (!string.IsNullOrWhiteSpace(toLoad.Description))
            if (!string.IsNullOrWhiteSpace(toLoad.Comment) &&
                !toLoad.Comment.Equals(toLoad.Description, StringComparison.OrdinalIgnoreCase))
                userSummary += $" {toLoad.Description.Trim()}";

        UserSummary = userSummary;
    }
}