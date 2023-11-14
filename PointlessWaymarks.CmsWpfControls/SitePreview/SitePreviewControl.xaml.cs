using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.SitePreview;

/// <summary>
///     Interaction logic for SitePreviewControl.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class SitePreviewControl
{
    public SitePreviewControl()
    {
        InitializeComponent();

        DataContext = PreviewContext;
    }

    public SitePreviewContext? PreviewContext { get; set; }

    private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        PreviewContext?.NewWindowRequestedAction?.Invoke(e);
    }

    private void CoreWebView2OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (PreviewContext == null) return;
        PreviewContext.CurrentDocumentTitle = SitePreviewWebView.CoreWebView2.DocumentTitle.TruncateWithEllipses(24);
    }

    private async void InitializeAsync()
    {
        // must create a data folder if running out of a secured folder that can't write like Program Files
        var env = await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(Path.GetTempPath(),
            "PointWaymarksCms_SitePreviewBrowserData"));

        await ThreadSwitcher.ResumeForegroundAsync();
        // Note this waits until the first page is navigated!
        await SitePreviewWebView.EnsureCoreWebView2Async(env);
        if (PreviewContext != null) SitePreviewWebView.CoreWebView2.Navigate(PreviewContext.InitialPage);
        SitePreviewWebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        SitePreviewWebView.CoreWebView2.NavigationCompleted += CoreWebView2OnNavigationCompleted;
    }

    public void LoadData()
    {
        InitializeAsync();
    }

    private void SitePreviewControl_OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SitePreviewContext context)
        {
            PreviewContext = context;
            context.WebViewGui = this;
            LoadData();
        }
    }

    private void SitePreviewWebView_OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Uri))
        {
            e.Cancel = true;
            PreviewContext!.StatusContext.ToastError("Blank URL for navigation?");
            return;
        }

        if (!e.Uri.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            PreviewContext!.StatusContext.ToastError("This window only supports http and https (no ftp, etc.)");
            return;
        }

        //The preview server rewrites html files so that links should point
        //to the localhost preview - this is to catch links loaded by javascript
        //that point to the site and redirect the link to localhost
        if (e.Uri.Contains(PreviewContext!.SiteUrl, StringComparison.CurrentCultureIgnoreCase) &&
            !e.Uri.Contains(PreviewContext.PreviewServerHost))
        {
            var rewrittenUrl = e.Uri.Replace("https://", "http://", StringComparison.OrdinalIgnoreCase)
                .Replace($"//{PreviewContext.SiteUrl}", $"//{PreviewContext.PreviewServerHost}",
                    StringComparison.OrdinalIgnoreCase);
            e.Cancel = true;

            SitePreviewWebView.CoreWebView2.Navigate(rewrittenUrl);
            return;
        }

        Uri parsedUri;
        try
        {
            parsedUri = new Uri(e.Uri);
        }
        catch (Exception exception)
        {
            e.Cancel = true;
            PreviewContext?.StatusContext.ToastError($"Trouble parsing {e.Uri}? {exception.Message}");
            return;
        }

        if (!string.Equals(parsedUri.Authority, PreviewContext.PreviewServerHost,
                StringComparison.CurrentCultureIgnoreCase))
        {
            e.Cancel = true;
            ProcessHelpers.OpenUrlInExternalBrowser(e.Uri);
            //PreviewContext.StatusContext.ToastSuccess($"Sending external link {e.Uri} to the default browser.");
            PreviewContext.TextBarAddress = new Uri(PreviewContext.CurrentAddress).PathAndQuery;
            return;
        }

        PreviewContext.TextBarAddress = new Uri(e.Uri).PathAndQuery;
    }
}