using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
