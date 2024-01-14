using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Xaml.Behaviors;
using PointlessWaymarks.CommonTools;
using Serilog;

namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

/// <summary>
///     This behavior targets dealing with string Html binding and sending Json data to a WebView2 - the challenges to this
///     really arise from the WebView2's initialization which can be distinctly delayed on first load, and the time to load
///     the Html during which the Javascript may not yet be active to process Json data. This is a particular pain point in
///     a View Model setup where the model doesn't have direct access to the WebView2.
/// </summary>
public class WebViewGeneratedVirtualDomainBehavior : Behavior<WebView2>
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
        typeof(IWebViewMessenger), typeof(WebViewGeneratedVirtualDomainBehavior),
        new PropertyMetadata(default(IWebViewMessenger), OnWebViewManagerChanged));

    private DirectoryInfo _targetDirectory;
    private string _virtualDomain;

    private bool _webViewHasLoaded;


    public IWebViewMessenger WebViewMessenger
    {
        get => (IWebViewMessenger)GetValue(WebViewMessengerProperty);
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
            new MessageFromWebView(args.WebMessageAsJson));
    }

    public event EventHandler<MessageFromWebView>? OnJsonFromWebView;

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
        if (d is WebViewGeneratedVirtualDomainBehavior bindingBehavior &&
            e.NewValue is IWebViewMessenger newMessenger)
        {
            bindingBehavior.OnJsonFromWebView += newMessenger.FromWebView;
            bindingBehavior.WebViewMessenger = newMessenger;
            newMessenger.ToWebView.Processor = bindingBehavior.ToWebViewMessageProcessor;

            await ThreadSwitcher.ResumeForegroundAsync();
            bindingBehavior.WebViewMessenger.ToWebView.Suspend(!bindingBehavior._webViewHasLoaded);
        }
    }

    private async Task ProcessWebViewFileBuilder(FileBuilder fileBuilder)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var fileList = new List<string>();

        foreach (var loopCreate in fileBuilder.Create)
        {
            var targetFile = Path.Combine(_targetDirectory.FullName, loopCreate.filename);
            if (File.Exists(targetFile)) File.Delete(targetFile);
            await File.WriteAllTextAsync(targetFile,
                loopCreate.body.Replace("[[VirtualDomain]]", _virtualDomain, StringComparison.OrdinalIgnoreCase));
            fileList.Add(targetFile);
        }

        foreach (var loopCopy in fileBuilder.Copy)
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
                    nameof(WebViewGeneratedVirtualDomainBehavior));
            }
    }

    private async Task ProcessWebViewJson(JsonData jsonData)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        if (!string.IsNullOrWhiteSpace(jsonData.Json))
            AssociatedObject.CoreWebView2.PostWebMessageAsJson(jsonData.Json);
    }

    private async Task ProcessWebViewNavigation(NavigateTo navigateTo)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        if (navigateTo.WaitForScriptFinished) WebViewMessenger.ToWebView.Suspend(true);

        if (!string.IsNullOrWhiteSpace(navigateTo.Url))
            AssociatedObject.CoreWebView2.Navigate(
                $"https://{_virtualDomain}/{navigateTo.Url}");
    }

    private async Task ToWebViewMessageProcessor(ToWebViewRequest arg)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await arg.Match(
            ProcessWebViewFileBuilder,
            ProcessWebViewNavigation,
            ProcessWebViewJson
        );
    }
}