using System.Windows;
using Microsoft.Web.WebView2.Core;
using OneOf;
using OneOf.Types;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.SitePreview;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class SitePreviewContext : DependencyObject
{
    public SitePreviewContext(string siteUrl, string localSiteFolder, string siteName, string previewServerHost,
        StatusControlContext statusContext, string initialUrl = "")
    {
        StatusContext = statusContext;

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

        NavigationItems =
        [
            new SitePreviewMenuNavigationItem("Search", $"http://{PreviewServerHost}/AllContentList.html"),
            new SitePreviewMenuNavigationItem("Content Gallery", $"http://{PreviewServerHost}/LatestContent.html"),
            new SitePreviewMenuNavigationItem("Camera Roll",
                $"http://{PreviewServerHost}/Photos/Galleries/CameraRoll.html"),
            new SitePreviewMenuNavigationItem("Tags", $"http://{PreviewServerHost}/Tags/AllTagsList.html"),
            new SitePreviewMenuNavigationItem("File Search", $"http://{PreviewServerHost}/Files/FileList.html"),
            new SitePreviewMenuNavigationItem("GeoJson Search", $"http://{PreviewServerHost}/GeoJson/GeoJsonList.html"),
            new SitePreviewMenuNavigationItem("Line Search", $"http://{PreviewServerHost}/Lines/LineList.html"),
            new SitePreviewMenuNavigationItem("Link Search", $"http://{PreviewServerHost}/Links/LinkList.html"),
            new SitePreviewMenuNavigationItem("Note Search", $"http://{PreviewServerHost}/Notes/NoteList.html"),
            new SitePreviewMenuNavigationItem("Photo Search", $"http://{PreviewServerHost}/Photos/PhotoList.html"),
            new SitePreviewMenuNavigationItem("Point Search", $"http://{PreviewServerHost}/Points/PointsList.html"),
            new SitePreviewMenuNavigationItem("Post Search", $"http://{PreviewServerHost}/Posts/PostList.html"),
            new SitePreviewMenuNavigationItem("Trail Search", $"http://{PreviewServerHost}/Trails/TrailList.html"),
            new SitePreviewMenuNavigationItem("Video Search", $"http://{PreviewServerHost}/Videos/VideoList.html"),
        ];

        CurrentAddress = InitialPage;
    }

    public string CurrentAddress { get; set; }
    public string CurrentDocumentTitle { get; set; } = string.Empty;
    public string InitialPage { get; set; }
    public Func<Task<OneOf<Success<byte[]>, Error<string>>>>? JpgScreenshotFunction { get; set; }
    public string LocalSiteFolder { get; set; }

    public List<SitePreviewMenuNavigationItem> NavigationItems { get; set; }
    public Action<CoreWebView2NewWindowRequestedEventArgs>? NewWindowRequestedAction { get; set; }
    public string PreviewServerHost { get; set; }
    public string SiteMappingNote { get; set; }
    public string SiteName { get; set; }
    public string SiteUrl { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public int TestNumber { get; set; }
    public string? TextBarAddress { get; set; }
    public SitePreviewControl? WebViewGui { get; set; }

    [BlockingCommand]
    private async Task JpgScreenshot()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (JpgScreenshotFunction == null)
        {
            await StatusContext.ToastError("Screenshot function not available...");
            return;
        }

        var screenshotResult = await JpgScreenshotFunction();

        if (screenshotResult.IsT0)
            await WebViewToJpg.SaveByteArrayAsJpg(screenshotResult.AsT0.Value, string.Empty, StatusContext);
        else
            await StatusContext.ToastError(screenshotResult.AsT1.Value);
    }

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
    private async Task TryNavigateToUrl(string url)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        WebViewGui?.SitePreviewWebView.CoreWebView2.Navigate(url);
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