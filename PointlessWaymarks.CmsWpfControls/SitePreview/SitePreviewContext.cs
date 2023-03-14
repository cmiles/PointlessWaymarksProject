using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Wpf;
using PointlessWaymarks.CommonTools;
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
    [ObservableProperty] private string? _textBarAddress;
    [ObservableProperty] private RelayCommand _tryGoBackNavigationCommand;
    [ObservableProperty] private RelayCommand _tryGoForwardNavigationCommand;
    [ObservableProperty] private RelayCommand _tryNavigateHomeCommand;
    [ObservableProperty] private RelayCommand _tryRefreshCommand;
    [ObservableProperty] private RelayCommand _tryUserNavigationCommand;
    [ObservableProperty] private WebView2? _webViewGui;
    [ObservableProperty] private string _windowTitle;

    public SitePreviewContext(string siteUrl, string localSiteFolder, string siteName, string previewServerHost,
        StatusControlContext? statusContext)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _siteUrl = siteUrl;
        _localSiteFolder = localSiteFolder;
        _siteName = siteName;
        _previewServerHost = previewServerHost;

        _initialPage = string.IsNullOrEmpty(InitialPage) ? $"http://{previewServerHost}/index.html" : InitialPage;

        _windowTitle = string.IsNullOrWhiteSpace(SiteName)
            ? $"Preview - {LocalSiteFolder} is mapped to {SiteUrl}"
            : $"{SiteName} - {LocalSiteFolder} is mapped to {SiteUrl}";

        _currentAddress = InitialPage;

        _tryUserNavigationCommand = StatusContext.RunBlockingTaskCommand(TryUserNavigation);
        _tryGoBackNavigationCommand = StatusContext.RunBlockingTaskCommand(TryGoBackNavigation);
        _tryGoForwardNavigationCommand = StatusContext.RunBlockingTaskCommand(TryGoForwardNavigation);
        _tryRefreshCommand = StatusContext.RunBlockingTaskCommand(TryRefresh);
        _tryNavigateHomeCommand = StatusContext.RunBlockingTaskCommand(TryNavigateHome);
    }

    private async Task TryGoBackNavigation()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        if (WebViewGui?.CoreWebView2.CanGoBack ?? false) WebViewGui.CoreWebView2.GoBack();
    }

    private async Task TryGoForwardNavigation()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        if (WebViewGui?.CoreWebView2.CanGoForward ?? false) WebViewGui.CoreWebView2.GoForward();
    }

    private async Task TryNavigateHome()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.CoreWebView2.Navigate(InitialPage);
    }

    private async Task TryRefresh()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.CoreWebView2.Reload();
    }

    private async Task TryUserNavigation()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.CoreWebView2.Navigate($"http://{StringTools.UrlCombine(SiteUrl, TextBarAddress ?? string.Empty)}");
    }
}