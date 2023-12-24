using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace PointlessWaymarks.CmsWpfControls.PhotoContentEditor;

public partial class LocationChooserWindow : Window
{
    public LocationChooserWindow()
    {
        InitializeComponent();
    }

    private void PointContentWebView_OnCoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        throw new NotImplementedException();
    }
}