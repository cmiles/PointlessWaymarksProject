using Microsoft.Web.WebView2.Core;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.FeedReaderGui.Controls;

public partial class FeedItemListControl
{
    public FeedItemListControl()
    {
        InitializeComponent();
    }

    private void BodyContentWebView_OnCoreWebView2InitializationCompleted(object? sender,
        CoreWebView2InitializationCompletedEventArgs e)
    {
        BodyContentWebView.CoreWebView2.BasicAuthenticationRequested += (o, args) =>
        {
            if (DataContext is not FeedItemListContext context) return;

            args.Response.UserName = context.DisplayBasicAuthUsername;
            args.Response.Password = context.DisplayBasicAuthPassword;
        };
    }

    private void BodyContentWebView_OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        var context = DataContext as FeedItemListContext;

        if (string.IsNullOrWhiteSpace(context?.DisplayUrl)) return;

        if (e.Uri.Equals(context.DisplayUrl)) return;

        e.Cancel = true;

        ProcessHelpers.OpenUrlInExternalBrowser(e.Uri);
    }

    private void RssContentWebView_OnCoreWebView2InitializationCompleted(object? sender,
        CoreWebView2InitializationCompletedEventArgs e)
    {
        RssContentWebView.CoreWebView2.BasicAuthenticationRequested += (o, args) =>
        {
            if (DataContext is not FeedItemListContext context) return;

            args.Response.UserName = context.DisplayBasicAuthUsername;
            args.Response.Password = context.DisplayBasicAuthPassword;
        };
    }
}