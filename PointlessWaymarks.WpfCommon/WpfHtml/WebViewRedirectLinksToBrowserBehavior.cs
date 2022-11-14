﻿using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Xaml.Behaviors;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.WpfCommon.WpfHtml;

public class WebViewRedirectLinksToBrowserBehavior : Behavior<WebView2>
{
    protected override void OnAttached()
    {
        AssociatedObject.NavigationStarting += WebView_OnNavigationStarting;
    }

    private void WebView_OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!e.IsUserInitiated) return;

        if (e.Uri != null && e.Uri.Contains("about:blank"))
        {
            e.Cancel = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(e.Uri)) return;

        var localTempHtml = new Uri(FileSystemHelpers.TempStorageHtmlDirectory().FullName).AbsoluteUri;

        if (e.Uri.Contains(localTempHtml)) return;

        e.Cancel = true;
        ProcessHelpers.OpenUrlInExternalBrowser(e.Uri);
    }
}