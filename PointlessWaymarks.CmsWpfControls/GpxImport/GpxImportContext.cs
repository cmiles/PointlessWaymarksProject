using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using NetTopologySuite.IO;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.GpxImport
{
    [ObservableObject]
    public partial class GpxImportContext
    {
    [ObservableProperty] private StatusControlContext _statusContext;
        
    }

    [ObservableObject]
    public partial class GpxImportPoint : IGpxImportListItem
    {
        [ObservableProperty] private GpxWaypoint _gpxWaypoint;
        [ObservableProperty] private bool _markedForImport;

    }

    public interface IGpxImportListItem : INotifyPropertyChanged
    {
        bool MarkedForImport { get; set; }
    } 
}
