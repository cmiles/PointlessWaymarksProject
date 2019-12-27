using System.Windows;

namespace TheLemmonWorkshopWpfControls.HtmlViewer
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