using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Xaml.Behaviors;
using PointlessWaymarks.CommonTools;
using Serilog;

namespace PointlessWaymarks.WpfCommon.WpfHtml;

/// <summary>
/// This behavior targets dealing with string Html binding and sending Json data to a WebView2 - the challenges to this
/// really arise from the WebView2's initialization which can be distinctly delayed on first load, and the time to load
/// the Html during which the Javascript may not yet be active to process Json data. This is a particular pain point in
/// a View Model setup where the model doesn't have direct access to the WebView2.
/// </summary>
public class WebViewHtmlStringAndJsonOnChangeBehavior : Behavior<WebView2>
{
    //<b:Interaction.Behaviors>
    //  <local:WebViewHtmlStringAndJsonOnChangeBehavior HtmlString = "{Binding PreviewHtml}" />
    //</b:Interaction.Behaviors>

    /// <summary>
    /// String HTML - when changed the WebView2 will be reloaded with the new string
    /// </summary>
    public static readonly DependencyProperty HtmlStringProperty = DependencyProperty.Register(nameof(HtmlString),
        typeof(string), typeof(WebViewHtmlStringAndJsonOnChangeBehavior),
        new PropertyMetadata(default(string), OnHtmlChanged));

    /// <summary>
    /// Monitored for new values - any new values will be sent to the WebView2.
    /// </summary>
    public static readonly DependencyProperty JsonDataProperty = DependencyProperty.Register(nameof(JsonData),
        typeof(string), typeof(WebViewHtmlStringAndJsonOnChangeBehavior),
        new PropertyMetadata(default(string), OnJsonDataChanged));

    private readonly List<FileInfo> _previousFiles = new();
    private FileInfo? _currentFile;
    private bool _loaded;

    public string CachedHtml { get; set; } = string.Empty;
    public Queue<string> CachedJson { get; set; } = new();
    public bool HtmlLoading { get; set; } = true;

    public string HtmlString
    {
        get => (string)GetValue(HtmlStringProperty);
        set => SetValue(HtmlStringProperty, value);
    }

    public string JsonData
    {
        get => (string)GetValue(JsonDataProperty);
        set => SetValue(JsonDataProperty, value);
    }

    protected override void OnAttached()
    {
        //Setup the WebView2 environment
        AssociatedObject.Loaded += OnLoaded;

        //See if there is any cached HTML and Json to Load
        AssociatedObject.CoreWebView2InitializationCompleted += OnReady;
    }

    /// <summary>
    /// Sets the Html for the WebView when the bound string changes - if the WebView2 is not yet ready to load cache the
    /// html. This method will NOT read from the cache, only write to it! The cache is a single value with only the latest
    /// html submitted.
    /// </summary>
    /// <param name="d"></param>
    /// <param name="e"></param>
    private static async void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebViewHtmlStringAndJsonOnChangeBehavior bindingBehavior)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            if (bindingBehavior.AssociatedObject.IsInitialized &&
                bindingBehavior.AssociatedObject.CoreWebView2 != null)
            {
                //A naive attempt to prevent Html and JsonData from overlapped loading
                bindingBehavior.HtmlLoading = true;

                //We only keep the latest value cached if needed until load - since we now can load string value from changes
                //the cache value is not valid - clear it.
                bindingBehavior.CachedHtml = string.Empty;
                try
                {
                    var newString = e.NewValue as string ??
                                    "<h2>...</h2>".ToHtmlDocumentWithLeaflet("...", string.Empty);

                    var newFile = new FileInfo(Path.Combine(
                        FileLocationTools.TempStorageHtmlDirectory().FullName,
                        $"TempHtml-{Guid.NewGuid()}.html"));
                    await File.WriteAllTextAsync(newFile.FullName, newString);
                    bindingBehavior.AssociatedObject.CoreWebView2.Navigate($"file:////{newFile.FullName}");

                    bindingBehavior._previousFiles.Add(newFile);
                    bindingBehavior._currentFile = newFile;

                    if (!bindingBehavior._previousFiles.Any()) return;

                    var filesToDelete = bindingBehavior._currentFile == null
                        ? bindingBehavior._previousFiles
                        : bindingBehavior._previousFiles.Except(bindingBehavior._currentFile.AsList()).ToList();

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

                bindingBehavior.HtmlLoading = false;
                bindingBehavior.ProcessJsonDataCache();
            }
            else
            {
                bindingBehavior.CachedHtml = e.NewValue as string ??
                                             "<h2>Loading...</h2>".ToHtmlDocumentWithLeaflet("...", string.Empty);
            }
        }
    }


    private static async void OnJsonDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebViewHtmlStringAndJsonOnChangeBehavior bindingBehavior)
            await PostNewJson(bindingBehavior, e.NewValue as string ?? string.Empty);
    }

    /// <summary>
    /// Setup the web environment.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_loaded)
        {
            _loaded = true;
            try
            {
                var webViewEnvironment = await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(
                    FileLocationTools.TempStorageHtmlDirectory().FullName));

                await ThreadSwitcher.ResumeForegroundAsync();
                await AssociatedObject.EnsureCoreWebView2Async(webViewEnvironment);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Log.Error(exception, "Error in the OnLoaded method with the WebView2.");
            }
        }
    }

    public void ProcessJsonDataCache()
    {
        var toProcess = CachedJson.ToList();
        CachedJson.Clear();

        foreach (var loopCache in toProcess)
            try
            {
                AssociatedObject.CoreWebView2.PostWebMessageAsJson(loopCache);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PostNewJson Error");
            }
    }

    /// <summary>
    /// When the WebView2 fires OnReady check if there is anything in the CachedHtml to load - if there is load it and
    /// wire up the NavigationCompleted event to process any cached Json.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnReady(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(CachedHtml))
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            AssociatedObject.NavigationCompleted += OnReadyHtmlCacheLoadCompleted;
            AssociatedObject.NavigateToString(CachedHtml);
        }
    }

    /// <summary>
    /// Fires when the OnReady Event of the WebView fires if CachedHtml is loaded this event is wired up to process the
    /// Json after the cached Html navigation completes - note this method detaches itself so the it doesn't fire on Subsequent
    /// NavigationCompleted events.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnReadyHtmlCacheLoadCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        HtmlLoading = false;
        ProcessJsonDataCache();
        AssociatedObject.NavigationCompleted -= OnReadyHtmlCacheLoadCompleted;
    }


    /// <summary>
    /// Processes Json on value change if the WebView2 appears to be loaded - otherwise caches Json values to load
    /// in a Queue.
    /// </summary>
    /// <param name="bindingBehavior"></param>
    /// <param name="toPost"></param>
    private static async Task PostNewJson(WebViewHtmlStringAndJsonOnChangeBehavior bindingBehavior, string toPost)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        if (bindingBehavior.AssociatedObject.IsInitialized && bindingBehavior.AssociatedObject.CoreWebView2 != null &&
            !bindingBehavior.HtmlLoading)
        {
            if (!string.IsNullOrWhiteSpace(toPost)) bindingBehavior.CachedJson.Enqueue(toPost);
            var toProcess = bindingBehavior.CachedJson.ToList();
            bindingBehavior.CachedJson.Clear();

            bindingBehavior.ProcessJsonDataCache();
        }
        else
        {
            bindingBehavior.CachedJson.Enqueue(toPost);
        }
    }
}