using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.HtmlViewer;

[NotifyPropertyChanged]
public partial class HtmlViewerContext
{
    public HtmlViewerContext()
    {
        StatusContext = new StatusControlContext();
    }

    public string HtmlString { get; set; } = string.Empty;
    public StatusControlContext StatusContext { get; set; }
}