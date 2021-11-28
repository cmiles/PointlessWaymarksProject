using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Web.WebView2.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.SitePreview;

[ObservableObject]
public partial class SitePreviewContext : DependencyObject
{
    [ObservableProperty] private string _currentAddress;
    [ObservableProperty] private string _initialPage;
    [ObservableProperty] private string _localSiteFolder;
    [ObservableProperty] private string _previewServerHost;
    [ObservableProperty] private string _siteName;
    [ObservableProperty] private string _siteUrl;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _textBarAddress;
    [ObservableProperty] private Command _tryGoBackNavigationCommand;
    [ObservableProperty] private Command _tryGoForwardNavigationCommand;
    [ObservableProperty] private Command _tryNavigateHomeCommand;
    [ObservableProperty] private Command _tryRefreshCommand;
    [ObservableProperty] private Command _tryUserNavigationCommand;
    [ObservableProperty] private WebView2 _webViewGui;
    [ObservableProperty] private string _windowTitle;

    public SitePreviewContext(string siteUrl, string localSiteFolder, string siteName, string previewServerHost,
        StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        SiteUrl = siteUrl;
        LocalSiteFolder = localSiteFolder;
        SiteName = siteName;
        PreviewServerHost = previewServerHost;

        InitialPage = string.IsNullOrEmpty(InitialPage) ? $"http://{previewServerHost}/index.html" : InitialPage;

        WindowTitle = string.IsNullOrWhiteSpace(SiteName)
            ? $"Preview - {LocalSiteFolder} is mapped to {SiteUrl}"
            : $"{SiteName} - {LocalSiteFolder} is mapped to {SiteUrl}";

        CurrentAddress = InitialPage;

        TryUserNavigationCommand = StatusContext.RunBlockingTaskCommand(TryUserNavigation);
        TryGoBackNavigationCommand = StatusContext.RunBlockingTaskCommand(TryGoBackNavigation);
        TryGoForwardNavigationCommand = StatusContext.RunBlockingTaskCommand(TryGoForwardNavigation);
        TryRefreshCommand = StatusContext.RunBlockingTaskCommand(TryRefresh);
        TryNavigateHomeCommand = StatusContext.RunBlockingTaskCommand(TryNavigateHome);
    }

    private async Task TryGoBackNavigation()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        if (WebViewGui.CoreWebView2.CanGoBack) WebViewGui.CoreWebView2.GoBack();
    }

    private async Task TryGoForwardNavigation()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        if (WebViewGui.CoreWebView2.CanGoForward) WebViewGui.CoreWebView2.GoForward();
    }

    private async Task TryNavigateHome()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui.CoreWebView2.Navigate(InitialPage);
    }

    private async Task TryRefresh()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui.CoreWebView2.Reload();
    }

    private async Task TryUserNavigation()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui.CoreWebView2.Navigate($"http://{StringHelpers.UrlCombine(SiteUrl, TextBarAddress)}");
    }
}