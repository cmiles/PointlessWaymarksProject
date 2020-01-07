using System.Windows;

namespace PointlessWaymarksCmsWpfControls.HtmlViewer
{
    public partial class HtmlViewerWindow : Window
    {
        public HtmlViewerWindow(string htmlString)
        {
            InitializeComponent();

            DataContext = new HtmlViewerContext {HtmlString = htmlString};
        }
    }
}