using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Xaml.Behaviors;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsWpfControls.Server;
using PointlessWaymarks.CmsWpfControls.SitePreview;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using Serilog;

namespace PointlessWaymarks.CmsWpfControls.WpfCmsHtml;

public class WebViewHtmlPostLocalPreviewBehavior : Behavior<WebView2CompositionControl>
{
    public static readonly DependencyProperty HtmlStringProperty = DependencyProperty.Register(nameof(HtmlString),
        typeof(string), typeof(WebViewHtmlPostLocalPreviewBehavior),
        new PropertyMetadata(default(string), OnHtmlChanged));
    
    public static readonly DependencyProperty AllowNonPreviewNavigationProperty = DependencyProperty.Register(
        nameof(AllowNonPreviewNavigation),
        typeof(bool), typeof(WebViewGeneratedVirtualDomainBehavior),
        new PropertyMetadata(false));
    //<b:Interaction.Behaviors>
    //  <local:WebViewHtmlStringBindingBehavior HtmlString = "{Binding PreviewHtml}" />
    //</b:Interaction.Behaviors>
    
    
    private readonly Guid _behaviorId = Guid.NewGuid();
    private string _cachedHtml = string.Empty;
    private bool _loaded;
    
    public bool AllowNonPreviewNavigation
    {
        get => (bool)GetValue(AllowNonPreviewNavigationProperty);
        set => SetValue(AllowNonPreviewNavigationProperty, value);
    }
    
    public string HtmlString
    {
        get => (string)GetValue(HtmlStringProperty);
        set => SetValue(HtmlStringProperty, value);
    }
    
    public async Task LoadPreviewPage(string htmlPreviewJson)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var htmlStream = htmlPreviewJson.ToMemoryStream();
        var request = AssociatedObject.CoreWebView2.Environment.CreateWebResourceRequest(
            PartialContentPreviewServer.PreviewServerLoadPreviewPageUrl,
            "POST", htmlStream, "Content-Type: application/json");
        AssociatedObject.CoreWebView2.NavigateWithWebResourceRequest(request);
    }
    
    protected override void OnAttached()
    {
        AssociatedObject.Loaded += OnLoaded;
        AssociatedObject.CoreWebView2InitializationCompleted += OnReady;
    }
    
    private static async void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebViewHtmlPostLocalPreviewBehavior bindingBehavior)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            
            if (bindingBehavior.AssociatedObject.IsInitialized &&
                bindingBehavior.AssociatedObject.CoreWebView2 != null)
            {
                bindingBehavior._cachedHtml = string.Empty;
                try
                {
                    var previewData = PartialContentPreviewServer.ServerLoadPreviewPage(bindingBehavior._behaviorId,
                        e.NewValue as string ?? string.Empty);
                    
                    await bindingBehavior.LoadPreviewPage(previewData);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "OnHtmlChanged Exception");
                }
            }
            else
            {
                bindingBehavior._cachedHtml = e.NewValue as string ?? string.Empty;
            }
        }
    }
    
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_loaded)
        {
            _loaded = true;
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(Path.GetTempPath(),
                    "PointWaymarksCms_SitePreviewBrowserData"));
                
                await ThreadSwitcher.ResumeForegroundAsync();
                await AssociatedObject.EnsureCoreWebView2Async(env);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Log.Error(exception, "Error in the OnLoaded method with the WebView2.");
            }
        }
    }
    
    private async void OnReady(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_cachedHtml))
        {
            var previewData = PartialContentPreviewServer.ServerLoadPreviewPage(_behaviorId,
                _cachedHtml);
            await LoadPreviewPage(previewData);
            return;
        }
        
        AssociatedObject.NavigationStarting += WebView_OnNavigationStarting;
    }
    
    private async void WebView_OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        
        //Not supporting this atm
        if (e.NavigationKind is CoreWebView2NavigationKind.Reload or CoreWebView2NavigationKind.BackOrForward)
        {
            e.Cancel = true;
            return;
        }
        
        var navigationUri = new Uri(e.Uri);
        
        //Primary Navigation - always allow
        if (navigationUri.AbsolutePath.EndsWith("loadpreviewpage", StringComparison.OrdinalIgnoreCase)
            || navigationUri.AbsolutePath.Contains("showpreviewpage", StringComparison.OrdinalIgnoreCase))
            return;
        
        if (AllowNonPreviewNavigation) return;

        //There is an element of guessing here that localhost means on-site in the Previews this behavior targets
        if (navigationUri.Host.Equals("localhost", StringComparison.InvariantCultureIgnoreCase))
        {
            //Careful with Threading - if you await the Foreground thread here there is a possibility that
            //that your methods will run but that Navigation won't be correctly cancelled...
            e.Cancel = true;
            
            var newPreview =
                await SiteOnDiskPreviewWindow.CreateInstance(
                    $"{UserSettingsSingleton.CurrentSettings().SiteUrl()}{navigationUri.AbsolutePath}");
            await newPreview.PositionWindowAndShowOnUiThread();
            return;
        }
        
        ProcessHelpers.OpenUrlInExternalBrowser(e.Uri);
        e.Cancel = true;
        return;
    }
}