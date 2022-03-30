using Microsoft.Toolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportWaypoint : IGpxImportListItem
{
    [ObservableProperty] private Guid _displayId;
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private bool _replaceElevationOnImport;
    [ObservableProperty] private GpxWaypoint _waypoint;

    public async Task Load(GpxWaypoint toLoad, IProgress<string> progress = null)
    {
        DisplayId = Guid.NewGuid();
        Waypoint = toLoad;
    }
}