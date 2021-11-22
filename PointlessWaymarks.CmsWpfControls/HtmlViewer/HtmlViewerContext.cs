using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.HtmlViewer;

[ObservableObject]
public partial class HtmlViewerContext
{
    [ObservableProperty] private string _htmlString;
    [ObservableProperty] private StatusControlContext _statusContext;

    public HtmlViewerContext()
    {
        StatusContext = new StatusControlContext();
    }
}