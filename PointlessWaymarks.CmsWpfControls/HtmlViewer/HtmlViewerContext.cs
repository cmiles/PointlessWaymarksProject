using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.HtmlViewer;

public partial class HtmlViewerContext : ObservableObject
{
    [ObservableProperty] private string _htmlString;
    [ObservableProperty] private StatusControlContext _statusContext;

    public HtmlViewerContext()
    {
        StatusContext = new StatusControlContext();
    }
}