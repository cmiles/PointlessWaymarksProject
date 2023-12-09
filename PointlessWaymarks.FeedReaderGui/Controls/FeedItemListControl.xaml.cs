using System.Windows;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.Web.WebView2.Core;

namespace PointlessWaymarks.FeedReaderGui.Controls;

public partial class FeedItemListControl
{
    public FeedItemListControl()
    {
        InitializeComponent();
    }

    private void BodyContentWebView_OnCoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        BodyContentWebView.CoreWebView2.BasicAuthenticationRequested += (o, args) =>
        {
            if (DataContext is not FeedItemListContext context) return;
            
            args.Response.UserName = context.DisplayBasicAuthUsername;
            args.Response.Password = context.DisplayBasicAuthPassword;
        };
    }
    
    private void RssContentWebView_OnCoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        RssContentWebView.CoreWebView2.BasicAuthenticationRequested += (o, args) =>
        {
            if (DataContext is not FeedItemListContext context) return;
            
            args.Response.UserName = context.DisplayBasicAuthUsername;
            args.Response.Password = context.DisplayBasicAuthPassword;
        };
    }
}