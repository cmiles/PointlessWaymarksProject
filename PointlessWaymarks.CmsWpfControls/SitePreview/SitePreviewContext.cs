using System.Windows;
using Microsoft.Web.WebView2.Core;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.SitePreview;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class SitePreviewContext : DependencyObject
{
    public SitePreviewContext(string siteUrl, string localSiteFolder, string siteName, string previewServerHost,
        StatusControlContext? statusContext, string initialUrl = "")
    {
        StatusContext = statusContext ?? new StatusControlContext();

        BuildCommands();

        SiteUrl = siteUrl;
        LocalSiteFolder = localSiteFolder;
        SiteName = siteName;
        PreviewServerHost = previewServerHost;

        if (string.IsNullOrWhiteSpace(initialUrl))
        {
            InitialPage = $"http://{previewServerHost}/index.html";
        }
        else
        {
            if (initialUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                InitialPage = initialUrl.Replace("https:", "http:", StringComparison.OrdinalIgnoreCase)
                    .Replace(siteUrl, previewServerHost, StringComparison.OrdinalIgnoreCase);
            else
                InitialPage = $"http://{previewServerHost}/{initialUrl}";
        }

        SiteMappingNote = string.IsNullOrWhiteSpace(SiteName)
            ? $"Preview - {LocalSiteFolder} is mapped to {SiteUrl}"
            : $"{SiteName} - {LocalSiteFolder} is mapped to {SiteUrl}";

        CurrentAddress = InitialPage;
    }

    public string CurrentAddress { get; set; }
    public string CurrentDocumentTitle { get; set; } = string.Empty;
    public string InitialPage { get; set; }
    public string LocalSiteFolder { get; set; }
    public Action<CoreWebView2NewWindowRequestedEventArgs>? NewWindowRequestedAction { get; set; }
    public string PreviewServerHost { get; set; }
    public string SiteMappingNote { get; set; }
    public string SiteName { get; set; }
    public string SiteUrl { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string? TextBarAddress { get; set; }
    public SitePreviewControl? WebViewGui { get; set; }

    [BlockingCommand]
    private async Task TryGoBackNavigation()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        if (WebViewGui?.SitePreviewWebView.CoreWebView2.CanGoBack ?? false)
            WebViewGui.SitePreviewWebView.CoreWebView2.GoBack();
    }

    [BlockingCommand]
    private async Task TryGoForwardNavigation()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        if (WebViewGui?.SitePreviewWebView.CoreWebView2.CanGoForward ?? false)
            WebViewGui.SitePreviewWebView.CoreWebView2.GoForward();
    }

    [BlockingCommand]
    private async Task TryNavigateHome()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.SitePreviewWebView.CoreWebView2.Navigate(InitialPage);
    }

    [BlockingCommand]
    private async Task TryNavigateToCameraRollGallery()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.SitePreviewWebView.CoreWebView2.Navigate(
            $"http://{PreviewServerHost}/Photos/Galleries/CameraRoll.html");
    }

    [BlockingCommand]
    private async Task TryNavigateToLatestContentGallery()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.SitePreviewWebView.CoreWebView2.Navigate($"http://{PreviewServerHost}/LatestContent.html");
    }

    [BlockingCommand]
    private async Task TryNavigateToLinkList()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.SitePreviewWebView.CoreWebView2.Navigate($"http://{PreviewServerHost}/Links/LinkList.html");
    }

    [BlockingCommand]
    private async Task TryNavigateToSearchPage()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.SitePreviewWebView.CoreWebView2.Navigate($"http://{PreviewServerHost}/AllContentList.html");
    }

    [BlockingCommand]
    private async Task TryNavigateToTagList()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.SitePreviewWebView.CoreWebView2.Navigate($"http://{PreviewServerHost}/Tags/AllTagsList.html");
    }

    [BlockingCommand]
    private async Task TryRefresh()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.SitePreviewWebView.CoreWebView2.Reload();
    }

    [BlockingCommand]
    private async Task TryUserNavigation()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.SitePreviewWebView.CoreWebView2.Navigate(
            $"http://{StringTools.UrlCombine(SiteUrl, TextBarAddress ?? string.Empty)}");
    }
}