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
public class WebViewHtmlStringAndJsonMessagingBehaviorV02 : Behavior<WebView2>
{
    // Example Usage in Xaml
    // <b:Interaction.Behaviors>
    //      <wpfHtml:WebViewHtmlStringAndJsonMessagingBehavior WebViewJsonMessenger="{Binding .}" HtmlString="{Binding MapHtml}" />
    // </b:Interaction.Behaviors>

    /// <summary>
    ///     String HTML - when changed the WebView2 will be reloaded with the new string
    /// </summary>
    public static readonly DependencyProperty WebViewMessengerProperty = DependencyProperty.Register(
        nameof(WebViewMessenger),
        typeof(IWebViewMessengerV02), typeof(WebViewHtmlStringAndJsonMessagingBehaviorV02),
        new PropertyMetadata(default(IWebViewMessenger), OnWebViewManagerChanged));

    private WebViewMessageV02? _currentMessage;
    private DirectoryInfo _targetDirectory;
    private string _virtualDomain;

    private bool _webViewHasLoaded;


    public IWebViewMessengerV02 WebViewMessenger
    {
        get => (IWebViewMessengerV02)GetValue(WebViewMessengerProperty);
        set => SetValue(WebViewMessengerProperty, value);
    }

    protected override void OnAttached()
    {
        _targetDirectory = UniqueFileTools
            .UniqueRandomLetterNameDirectory(FileLocationTools.TempStorageHtmlDirectory().FullName, 4);

        _virtualDomain = $"local-{_targetDirectory.Name}.pointlesswaymarks.com";

        AssociatedObject.Loaded += OnLoaded;
    }

    /// <summary>
    ///     Transforms and redirects the WebView2 Message Received Event to this object's OnJsonFromWebView event
    /// </summary>
    /// <param name="o"></param>
    /// <param name="args"></param>
    private async void OnCoreWebView2OnWebMessageReceived(object? o, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (args.WebMessageAsJson.Contains("scriptFinished"))
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            WebViewMessenger.ToWebView.Suspend(false);
            return;
        }

        OnJsonFromWebView?.Invoke(this,
            new WebViewMessage(args.WebMessageAsJson));
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
                AssociatedObject.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    $"local-{_targetDirectory.Name}.pointlesswaymarks.com",
                    _targetDirectory.FullName
                    , CoreWebView2HostResourceAccessKind.Allow);

                AssociatedObject.CoreWebView2.WebMessageReceived += OnCoreWebView2OnWebMessageReceived;

                await ThreadSwitcher.ResumeForegroundAsync();
                WebViewMessenger.ToWebView.Suspend(false);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Log.Error(exception, "Error in the OnLoaded method with the WebView2.");
            }
        }
    }

    private static async void OnWebViewManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebViewHtmlStringAndJsonMessagingBehaviorV02 bindingBehavior &&
            e.NewValue is IWebViewMessengerV02 newMessenger)
        {
            bindingBehavior.OnJsonFromWebView += newMessenger.JsonFromWebView;
            bindingBehavior.WebViewMessenger = newMessenger;
            newMessenger.ToWebView.Processor = bindingBehavior.ToWebViewMessageProcessor;

            await ThreadSwitcher.ResumeForegroundAsync();
            bindingBehavior.WebViewMessenger.ToWebView.Suspend(!bindingBehavior._webViewHasLoaded);
        }
    }

    private async Task ProcessWebViewFileBuilder(WebViewFileBuilder webViewFileBuilder)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var fileList = new List<string>();

        foreach (var loopCreate in webViewFileBuilder.Create)
        {
            var targetFile = Path.Combine(_targetDirectory.FullName, loopCreate.filename);
            if (File.Exists(targetFile)) File.Delete(targetFile);
            await File.WriteAllTextAsync(targetFile,
                loopCreate.body.Replace("[[VirtualDomain]]", _virtualDomain, StringComparison.OrdinalIgnoreCase));
            fileList.Add(targetFile);
        }

        foreach (var loopCopy in webViewFileBuilder.Copy)
        {
            var targetFile = Path.Combine(_targetDirectory.FullName, Path.GetFileName(loopCopy));
            if (File.Exists(targetFile)) File.Delete(targetFile);
            File.Copy(loopCopy, targetFile);
            fileList.Add(targetFile);
        }

        var currentFiles = _targetDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly).Select(x => x.FullName)
            .ToList();
        var toDelete = fileList.Except(currentFiles).ToList();

        foreach (var loopDelete in toDelete)
            try
            {
                File.Delete(loopDelete);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Log.ForContext("ignoredException", e.ToString()).Debug(
                    "{method} - Temporary File Delete Error - Silent Error, Continuing...",
                    nameof(WebViewHtmlStringAndJsonMessagingBehaviorV02));
            }
    }

    private async Task ProcessWebViewJson(WebViewJson webViewJson)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        if (!string.IsNullOrWhiteSpace(webViewJson.Json))
            AssociatedObject.CoreWebView2.PostWebMessageAsJson(webViewJson.Json);
    }

    private async Task ProcessWebViewNavigation(WebViewNavigation webViewNavigation)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        if (webViewNavigation.WaitForScriptFinished) WebViewMessenger.ToWebView.Suspend(true);

        if (!string.IsNullOrWhiteSpace(webViewNavigation.NavigateTo))
            AssociatedObject.CoreWebView2.Navigate(
                $"https://{_virtualDomain}/{webViewNavigation.NavigateTo}");
    }

    private async Task ToWebViewMessageProcessor(WebViewMessageV02 arg)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        _currentMessage = arg;

        await arg.Request.Match(
            ProcessWebViewFileBuilder,
            ProcessWebViewNavigation,
            ProcessWebViewJson
        );
    }
}