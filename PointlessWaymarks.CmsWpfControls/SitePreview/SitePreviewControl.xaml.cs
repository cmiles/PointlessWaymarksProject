using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.Web.WebView2.Core;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.SitePreview;

/// <summary>
///     Interaction logic for SitePreviewControl.xaml
/// </summary>
public partial class SitePreviewControl : INotifyPropertyChanged
{
    private SitePreviewContext _previewContext;
    private bool _loaded;

    public SitePreviewControl()
    {
        InitializeComponent();

        DataContext = PreviewContext;
    }

    public SitePreviewContext PreviewContext
    {
        get => _previewContext;
        set
        {
            if (Equals(value, _previewContext)) return;
            _previewContext = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private async void InitializeAsync()
    {
        //if (!_loaded)
        //{
        //    _loaded = true;

        // must create a data folder if running out of a secured folder that can't write like Program Files
        var env = await CoreWebView2Environment.CreateAsync(
            userDataFolder: Path.Combine(Path.GetTempPath(), "PointWaymarksCms_SitePreviewBrowserData"));

        await ThreadSwitcher.ResumeForegroundAsync();
        // Note this waits until the first page is navigated!
        await SitePreviewWebView.EnsureCoreWebView2Async(env);

        SitePreviewWebView.CoreWebView2.Navigate(PreviewContext.InitialPage);
        //}
    }

    public void LoadData()
    {
        InitializeAsync();
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    private void SitePreviewControl_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SitePreviewContext context)
        {
            PreviewContext = context;
            context.WebViewGui = SitePreviewWebView;
            LoadData();
        }
    }

    private void SitePreviewWebView_OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Uri))
        {
            e.Cancel = true;
            PreviewContext.StatusContext.ToastError("Blank URL for navigation?");
            return;
        }

        if (!e.Uri.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            PreviewContext.StatusContext.ToastError("This window only supports http and https (no ftp, etc.)");
            return;
        }

        //The preview server rewrites html files so that links should point
        //to the localhost preview - this is to catch links loaded by javascript
        //that point to the site and redirect the link to localhost
        if (e.Uri.Contains(PreviewContext.SiteUrl, StringComparison.CurrentCultureIgnoreCase) &&
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
            PreviewContext.StatusContext.ToastError($"Trouble parsing {e.Uri}? {exception.Message}");
            return;
        }

        if (!string.Equals(parsedUri.Authority, PreviewContext.PreviewServerHost,
                StringComparison.CurrentCultureIgnoreCase))
        {
            e.Cancel = true;
            ProcessHelpers.OpenUrlInExternalBrowser(e.Uri);
            PreviewContext.StatusContext.ToastError($"Sending external link {e.Uri} to the default browser.");
            PreviewContext.TextBarAddress = new Uri(PreviewContext.CurrentAddress).PathAndQuery;
            return;
        }

        PreviewContext.TextBarAddress = new Uri(e.Uri).PathAndQuery;
    }
}