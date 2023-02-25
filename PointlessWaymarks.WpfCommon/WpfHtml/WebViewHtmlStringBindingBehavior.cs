using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Xaml.Behaviors;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.WpfCommon.WpfHtml;

public class WebViewHtmlStringBindingBehavior : Behavior<WebView2>
{
    public static readonly DependencyProperty HtmlStringProperty = DependencyProperty.Register(nameof(HtmlString),
        typeof(string), typeof(WebViewHtmlStringBindingBehavior),
        new PropertyMetadata(default(string), OnHtmlChanged));

    private readonly List<FileInfo> _previousFiles = new();
    private bool _loaded;

    public string CachedHtml { get; set; } = string.Empty;

    public string HtmlString
    {
        get => (string)GetValue(HtmlStringProperty);
        set => SetValue(HtmlStringProperty, value);
    }

    protected override void OnAttached()
    {
        AssociatedObject.Loaded += OnLoaded;
        AssociatedObject.CoreWebView2InitializationCompleted += OnReady;
    }

    private static async void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebViewHtmlStringBindingBehavior bindingBehavior)
        {
            await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();

            if (bindingBehavior.AssociatedObject.IsInitialized &&
                bindingBehavior.AssociatedObject.CoreWebView2 != null)
            {
                bindingBehavior.CachedHtml = string.Empty;
                try
                {
                    var newString = e.NewValue as string ?? "<h2>...</h2>".ToHtmlDocumentWithLeaflet("...", string.Empty);

                    if (!string.IsNullOrWhiteSpace(newString))
                    {
                        var newFile = new FileInfo(Path.Combine(
                            FileSystemHelpers.TempStorageHtmlDirectory().FullName,
                            $"TempHtml-{Guid.NewGuid()}.html"));
                        await File.WriteAllTextAsync(newFile.FullName, newString);
                        bindingBehavior.AssociatedObject.CoreWebView2.Navigate($"file:////{newFile.FullName}");

                        if (!bindingBehavior._previousFiles.Any()) return;

                        foreach (var loopFiles in bindingBehavior._previousFiles)
                            try
                            {
                                loopFiles.Delete();
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine(exception);
                                throw;
                            }

                        bindingBehavior._previousFiles.ForEach(x => x.Refresh());
                        bindingBehavior._previousFiles.RemoveAll(x => !x.Exists);
                    }
                    else
                    {
                        bindingBehavior.AssociatedObject.NavigateToString(e.NewValue as string ??
                                                                          "<h2>Loading...</h2>".ToHtmlDocumentWithLeaflet(
                                                                              "...", string.Empty));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "OnHtmlChanged Exception");
                }
            }
            else
            {
                bindingBehavior.CachedHtml = e.NewValue as string ??
                                             "<h2>Loading...</h2>".ToHtmlDocumentWithLeaflet("...", string.Empty);
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
                var webViewEnvironment = await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(
                    FileSystemHelpers.TempStorageHtmlDirectory().FullName));

                await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();
                await AssociatedObject.EnsureCoreWebView2Async(webViewEnvironment);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Log.Error(exception, "Error in the OnLoaded method with the WebView2.");
            }
        }
    }

    private async void OnReady(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(CachedHtml))
        {
            await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();
            AssociatedObject.NavigateToString(CachedHtml);
        }
    }
}