using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Core;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.SitePreview;

[ObservableObject]
public partial class SitePreviewContext : DependencyObject
{
    [ObservableProperty] private string _currentAddress;
    [ObservableProperty] private string _currentDocumentTitle = string.Empty;
    [ObservableProperty] private string _initialPage;
    [ObservableProperty] private string _localSiteFolder;
    [ObservableProperty] private Action<CoreWebView2NewWindowRequestedEventArgs>? _newWindowRequestedAction;
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
    [ObservableProperty] private SitePreviewControl? _webViewGui;
    [ObservableProperty] private string _windowTitle;

    public SitePreviewContext(string siteUrl, string localSiteFolder, string siteName, string previewServerHost,
        StatusControlContext? statusContext, string initialPage = "")
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _siteUrl = siteUrl;
        _localSiteFolder = localSiteFolder;
        _siteName = siteName;
        _previewServerHost = previewServerHost;

        _initialPage = string.IsNullOrEmpty(initialPage) ? $"http://{previewServerHost}/index.html" : initialPage;

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
        if (WebViewGui?.SitePreviewWebView.CoreWebView2.CanGoBack ?? false)
            WebViewGui.SitePreviewWebView.CoreWebView2.GoBack();
    }

    private async Task TryGoForwardNavigation()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        if (WebViewGui?.SitePreviewWebView.CoreWebView2.CanGoForward ?? false)
            WebViewGui.SitePreviewWebView.CoreWebView2.GoForward();
    }

    private async Task TryNavigateHome()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.SitePreviewWebView.CoreWebView2.Navigate(InitialPage);
    }

    private async Task TryRefresh()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.SitePreviewWebView.CoreWebView2.Reload();
    }

    private async Task TryUserNavigation()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.SitePreviewWebView.CoreWebView2.Navigate(
            $"http://{StringTools.UrlCombine(SiteUrl, TextBarAddress ?? string.Empty)}");
    }
}