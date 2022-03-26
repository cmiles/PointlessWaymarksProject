using Microsoft.Toolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportTrack : IGpxImportListItem
{
    [ObservableProperty] private GpxTrack _gpxTrack;
    [ObservableProperty] private bool _markedForImport;
}