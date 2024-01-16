using System.Diagnostics;
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
    private string _virtualDomain = "localweb.pointlesswaymarks.com";

    private bool _webViewHasLoaded;


    public IWebViewMessenger? WebViewMessenger
    {
        get => (IWebViewMessenger)GetValue(WebViewMessengerProperty);
        set => SetValue(WebViewMessengerProperty, value);
    }

    protected override void OnAttached()
    {
        _targetDirectory = UniqueFileTools
            .UniqueRandomLetterNameDirectory(FileLocationTools.TempStorageHtmlDirectory().FullName, 4);

        _virtualDomain = $@"localweb.pointlesswaymarks.com\{_targetDirectory.Name}\";

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
            Debug.WriteLine("scriptFinished Received");
            await ThreadSwitcher.ResumeForegroundAsync();
            WebViewMessenger.ToWebView.Suspend(false);
            return;
        }

        WebViewMessenger.FromWebView.Enqueue(new FromWebViewMessage(args.WebMessageAsJson));
    }

    /// <summary>
    ///     Setup the web environment.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_webViewHasLoaded)
            try
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                await AssociatedObject.EnsureCoreWebView2Async();
                AssociatedObject.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    $"localweb.pointlesswaymarks.com",
                    _targetDirectory.Parent.FullName
                    , CoreWebView2HostResourceAccessKind.Allow);

                AssociatedObject.CoreWebView2.WebMessageReceived += OnCoreWebView2OnWebMessageReceived;

                await ThreadSwitcher.ResumeForegroundAsync();

                WebViewMessenger?.ToWebView.Suspend(false);
                _webViewHasLoaded = true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Log.Error(exception, "Error in the OnLoaded method with the WebView2.");
            }
    }

    private static async void OnWebViewManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebViewGeneratedVirtualDomainBehavior bindingBehavior &&
            e.NewValue is IWebViewMessenger newMessenger)
        {
            bindingBehavior.WebViewMessenger = newMessenger;

            bindingBehavior.WebViewMessenger.ToWebView.Suspend(!bindingBehavior._webViewHasLoaded);

            newMessenger.ToWebView.Processor = bindingBehavior.ToWebViewMessageProcessor;
        }
    }

    private async Task ProcessToWebViewFileBuilder(FileBuilder fileBuilder)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        Debug.WriteLine(
            $"{nameof(ProcessToWebViewFileBuilder)} - Tag {fileBuilder.RequestTag} - Create {fileBuilder.Create.Count}, Copy {fileBuilder.Copy.Count}, Overwrite {fileBuilder.TryToOverwriteExistingFiles}");

        foreach (var loopCreate in fileBuilder.Create)
        {
            var targetFile = Path.Combine(_targetDirectory.FullName, loopCreate.filename);

            if (!File.Exists(targetFile))
            {
                await File.WriteAllTextAsync(targetFile,
                    loopCreate.body.Replace("[[VirtualDomain]]", _virtualDomain, StringComparison.OrdinalIgnoreCase));
                continue;
            }

            //We know the file exists at this point
            if (!fileBuilder.TryToOverwriteExistingFiles) continue;

            try
            {
                File.Delete(targetFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Log.Verbose(
                    "Silent Error in {method} - Create Branch - trying to delete file {file}, some errors are expected...",
                    nameof(ProcessToWebViewFileBuilder), targetFile);
            }

            await File.WriteAllTextAsync(targetFile,
                loopCreate.body.Replace("[[VirtualDomain]]", _virtualDomain, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var loopCopy in fileBuilder.Copy)
        {
            var targetFile = Path.Combine(_targetDirectory.FullName, Path.GetFileName(loopCopy));

            if (!File.Exists(targetFile))
            {
                File.Copy(loopCopy, targetFile);
                continue;
            }

            //We know the file exists at this point
            if (!fileBuilder.TryToOverwriteExistingFiles) continue;

            try
            {
                File.Delete(targetFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Log.Verbose(
                    "Silent Error in {method} - Copy Branch - trying to delete file {file}, some errors are expected...",
                    nameof(ProcessToWebViewFileBuilder), targetFile);
            }

            File.Copy(loopCopy, targetFile);
        }
    }

    private async Task ProcessToWebViewJson(JsonData jsonData)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Debug.WriteLine(
            $"{nameof(ProcessToWebViewJson)} - Tag {jsonData.RequestTag} - Json Starts: {jsonData.Json[..Math.Min(jsonData.Json.Length, 100)]}");

        if (!string.IsNullOrWhiteSpace(jsonData.Json))
            AssociatedObject.CoreWebView2.PostWebMessageAsJson(jsonData.Json.Replace("[[VirtualDomain]]",
                _virtualDomain, StringComparison.OrdinalIgnoreCase));
    }

    private async Task ProcessToWebViewNavigation(NavigateTo navigateTo)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Debug.WriteLine(
            $"{nameof(ProcessToWebViewNavigation)} - Tag {navigateTo.RequestTag} - To: {navigateTo.Url} - WaitForScriptFinished: {navigateTo.WaitForScriptFinished}");

        if (navigateTo.WaitForScriptFinished) WebViewMessenger.ToWebView.Suspend(true);

        if (!string.IsNullOrWhiteSpace(navigateTo.Url))
            AssociatedObject.CoreWebView2.Navigate(
                $"https://{_virtualDomain}/{navigateTo.Url}");
    }

    private async Task ToWebViewMessageProcessor(ToWebViewRequest arg)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await arg.Match(
            ProcessToWebViewFileBuilder,
            ProcessToWebViewNavigation,
            ProcessToWebViewJson
        );
    }
}