using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Xaml.Behaviors;
using PointlessWaymarks.CommonTools;
using Serilog;

namespace PointlessWaymarks.WpfCommon.WpfHtml;

/// <summary>
///     This behavior targets dealing with string Html binding and sending Json data to a WebView2 - the challenges to this
///     really arise from the WebView2's initialization which can be distinctly delayed on first load, and the time to load
///     the Html during which the Javascript may not yet be active to process Json data. This is a particular pain point in
///     a View Model setup where the model doesn't have direct access to the WebView2.
/// </summary>
public class WebViewHtmlStringAndJsonMessagingBehavior : Behavior<WebView2>
{
    // Example Usage in Xaml
    // <b:Interaction.Behaviors>
    //      <wpfHtml:WebViewHtmlStringAndJsonMessagingBehavior WebViewJsonMessenger="{Binding .}" HtmlString="{Binding MapHtml}" />
    // </b:Interaction.Behaviors>

    /// <summary>
    ///     String HTML - when changed the WebView2 will be reloaded with the new string
    /// </summary>
    public static readonly DependencyProperty HtmlStringProperty = DependencyProperty.Register(nameof(HtmlString),
        typeof(string), typeof(WebViewHtmlStringAndJsonMessagingBehavior),
        new PropertyMetadata(default(string), OnHtmlChanged));

    public static readonly DependencyProperty WebViewMessengerProperty = DependencyProperty.Register(
        nameof(WebViewMessenger),
        typeof(IWebViewMessenger), typeof(WebViewHtmlStringAndJsonMessagingBehavior),
        new PropertyMetadata(default(IWebViewMessenger), OnWebViewManagerChanged));

    private readonly List<FileInfo> _previousTempFiles = new();
    private FileInfo? _currentTempFile;
    private bool _webViewHasLoaded;
    private string _webViewOnReadyCachedHtml = string.Empty;

    public string HtmlString
    {
        get => (string)GetValue(HtmlStringProperty);
        set => SetValue(HtmlStringProperty, value);
    }

    public IWebViewMessenger WebViewMessenger
    {
        get => (IWebViewMessenger)GetValue(WebViewMessengerProperty);
        set => SetValue(WebViewMessengerProperty, value);
    }

    protected override void OnAttached()
    {
        //Setup the WebView2 environment
        AssociatedObject.Loaded += OnLoaded;

        //See if there is any cached HTML and Json to Load
        AssociatedObject.CoreWebView2InitializationCompleted += OnReady;
    }

    /// <summary>
    ///     Transforms and redirects the WebView2 Message Received Event to this object's OnJsonFromWebView event
    /// </summary>
    /// <param name="o"></param>
    /// <param name="args"></param>
    private async void OnCoreWebView2OnWebMessageReceived(object? o, CoreWebView2WebMessageReceivedEventArgs args)
    {
        OnJsonFromWebView?.Invoke(this,
            new WebViewMessage(args.WebMessageAsJson));
    }

    /// <summary>
    ///     Sets the Html for the WebView when the bound string changes - if the WebView2 is not yet ready to load cache the
    ///     html.
    /// </summary>
    /// <param name="d"></param>
    /// <param name="e"></param>
    private static async void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebViewHtmlStringAndJsonMessagingBehavior bindingBehavior)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            if (bindingBehavior.AssociatedObject.IsInitialized &&
                bindingBehavior.AssociatedObject.CoreWebView2 != null)
            {
                //A naive attempt to prevent Html and JsonData from overlapped loading
                bindingBehavior.WebViewMessenger.JsonToWebView.Stop();

                try
                {
                    var newString = e.NewValue as string ??
                                    "<h2>...</h2>".ToHtmlDocumentWithLeaflet("...", string.Empty);

                    var newFile = new FileInfo(Path.Combine(
                        FileLocationTools.TempStorageHtmlDirectory().FullName,
                        $"TempHtml-{Guid.NewGuid()}.html"));
                    await File.WriteAllTextAsync(newFile.FullName, newString);
                    bindingBehavior.AssociatedObject.CoreWebView2.Navigate(
                        $"https://localcmshtml.pointlesswaymarks.com/{newFile.FullName}");

                    bindingBehavior._previousTempFiles.Add(newFile);
                    bindingBehavior._currentTempFile = newFile;

                    if (!bindingBehavior._previousTempFiles.Any()) return;

                    var filesToDelete = bindingBehavior._currentTempFile == null
                        ? bindingBehavior._previousTempFiles
                        : bindingBehavior._previousTempFiles.Except(bindingBehavior._currentTempFile.AsList()).ToList();

                    foreach (var loopFiles in filesToDelete)
                        try
                        {
                            loopFiles.Delete();
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                            throw;
                        }

                    filesToDelete.ForEach(x => x.Refresh());
                    filesToDelete.RemoveAll(x => !x.Exists);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "OnHtmlChanged Exception");
                }

                bindingBehavior.WebViewMessenger.JsonToWebView.Start();
            }
            else
            {
                bindingBehavior._webViewOnReadyCachedHtml = e.NewValue as string ??
                                                            "<h2>Loading...</h2>".ToHtmlDocumentWithLeaflet("...",
                                                                string.Empty);
            }
        }
    }


    public event EventHandler<WebViewMessage>? OnJsonFromWebView;

    /// <summary>
    ///     Setup the web environment.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_webViewHasLoaded)
        {
            _webViewHasLoaded = true;

            try
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                await AssociatedObject.EnsureCoreWebView2Async();
                AssociatedObject.CoreWebView2.SetVirtualHostNameToFolderMapping($"localcmshtml.pointlesswaymarks.com",
                    FileLocationTools.TempStorageHtmlDirectory().FullName, CoreWebView2HostResourceAccessKind.Allow);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Log.Error(exception, "Error in the OnLoaded method with the WebView2.");
            }
        }
    }

    /// <summary>
    ///     When the WebView2 fires OnReady check if there is anything in the CachedHtml to load - if there is load it and
    ///     wire up the NavigationCompleted event to process any cached Json.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnReady(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_webViewOnReadyCachedHtml))
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            AssociatedObject.NavigationCompleted += OnReadyHtmlCacheLoadCompleted;
            AssociatedObject.NavigateToString(_webViewOnReadyCachedHtml);
            _webViewOnReadyCachedHtml = string.Empty;
            AssociatedObject.CoreWebView2.WebMessageReceived += OnCoreWebView2OnWebMessageReceived;
        }
    }

    /// <summary>
    ///     Fires when the OnReady Event of the WebView fires if CachedHtml is loaded this event is wired up to process the
    ///     Json after the cached Html navigation completes - note this method detaches itself so the it doesn't fire on
    ///     Subsequent NavigationCompleted events.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnReadyHtmlCacheLoadCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        WebViewMessenger.JsonToWebView.Start();
        AssociatedObject.NavigationCompleted -= OnReadyHtmlCacheLoadCompleted;
    }

    private static void OnWebViewManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebViewHtmlStringAndJsonMessagingBehavior bindingBehavior &&
            e.NewValue is IWebViewMessenger newMessenger)
        {
            bindingBehavior.OnJsonFromWebView += newMessenger.JsonFromWebView;
            newMessenger.JsonToWebView.Processor = bindingBehavior.SendJson;
        }
    }

    private async Task SendJson(WebViewMessage arg)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(arg.Message))
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                AssociatedObject.CoreWebView2.PostWebMessageAsJson(arg.Message);
            }
        }
        catch (Exception ex)
        {
            Log.ForContext(nameof(arg), arg.SafeObjectDump()).Error(ex, "PostNewJson Error");
        }
    }
}