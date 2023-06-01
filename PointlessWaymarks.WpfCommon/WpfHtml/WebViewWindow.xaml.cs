using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.WpfCommon.WpfHtml;

/// <summary>
///     Interaction logic for WebViewWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class WebViewWindow
{
    public WebViewWindow()
    {
        InitializeComponent();

        DataContext = this;
    }

    public string PreviewGeoJsonDto { get; set; } = string.Empty;
    public string PreviewHtml { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = "Preview Map";
}