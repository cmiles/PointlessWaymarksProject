using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

namespace PointlessWaymarks.WpfCommon.WpfHtml;

/// <summary>
///     Interaction logic for WebViewWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class WebViewWindow : IWebViewMessenger
{
    public WebViewWindow()
    {
        InitializeComponent();

        FromWebView = new WorkQueue<FromWebViewMessage>();
        ToWebView = new WorkQueue<ToWebViewRequest>(true);

        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }
    public string WindowTitle { get; set; } = "Pointless Waymarks";

    public static async Task<WebViewWindow> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        return new WebViewWindow();
    }
}