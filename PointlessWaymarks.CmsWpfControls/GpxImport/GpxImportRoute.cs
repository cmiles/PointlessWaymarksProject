using Microsoft.Toolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportRoute : IGpxImportListItem
{
    [ObservableProperty] private bool _markedForImport;
    [ObservableProperty] private GpxRoute _route;
}