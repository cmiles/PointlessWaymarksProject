using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.WpfCommon.WpfHtml
{
    /// <summary>
    /// Interaction logic for WebViewWindow.xaml
    /// </summary>
    [ObservableObject]
    public partial class WebViewWindow
    {
        [ObservableProperty] private string _previewGeoJsonDto = string.Empty;
        [ObservableProperty] private string _previewHtml = string.Empty;
        [ObservableProperty] private string _windowTitle = "Preview Map";


        public WebViewWindow()
        {
            InitializeComponent();

            DataContext = this;
        }
    }
}
