using Microsoft.Toolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportWaypoint : IGpxImportListItem
{
    [ObservableProperty] private GpxWaypoint _gpxWaypoint;
    [ObservableProperty] private bool _markedForImport;
}