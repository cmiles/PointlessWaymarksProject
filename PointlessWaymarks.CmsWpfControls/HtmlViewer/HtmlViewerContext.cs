using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.HtmlViewer;

public partial class HtmlViewerContext : ObservableObject
{
    [ObservableProperty] private string _htmlString = string.Empty;
    [ObservableProperty] private StatusControlContext _statusContext;

    public HtmlViewerContext()
    {
        _statusContext = new StatusControlContext();
    }
}