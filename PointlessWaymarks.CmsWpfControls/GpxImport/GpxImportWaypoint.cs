using Microsoft.Toolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportWaypoint : IGpxImportListItem
{
    [ObservableProperty] private GpxWaypoint _waypoint;
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private bool _replaceElevationOnImport;
}