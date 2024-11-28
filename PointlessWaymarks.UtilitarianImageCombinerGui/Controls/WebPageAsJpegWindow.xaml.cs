using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Playwright;
using Microsoft.Web.WebView2.Core;
using OneOf;
using OneOf.Types;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.UtilitarianImageCombinerGui.Controls;

/// <summary>
///     Interaction logic for WebPageAsJpegWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class WebPageAsJpegWindow
{
    public WebPageAsJpegWindow(StatusControlContext statusContext, string initialUrl)
    {
        InitializeComponent();

        StatusContext = statusContext;
        UserUrl = initialUrl;
        BuildCommands();

        DataContext = this;
    }

    public Func<Task<OneOf<Success<byte[]>, Error<string>>>>? JpgScreenshotFunction { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string UserUrl { get; set; }

    private void ButtonNavigate_OnClick(object sender, RoutedEventArgs e)
    {
        var binding = UrlTextBox.GetBindingExpression(TextBox.TextProperty);
        binding?.UpdateSource();
    }

    public static async Task<WebPageAsJpegWindow> CreateInstance(StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        // Check if the clipboard contains a URL
        var clipboardUrl = string.Empty;

        if (Clipboard.ContainsText())
        {
            var clipboardText = Clipboard.GetText();
            if (Uri.TryCreate(clipboardText, UriKind.Absolute, out var uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                clipboardUrl = clipboardText;
        }

        // Use the clipboard URL if available, otherwise use the initialUrl parameter
        var urlToUse = !string.IsNullOrEmpty(clipboardUrl) ? clipboardUrl : string.Empty;

        var newInstance = new WebPageAsJpegWindow(statusContext, urlToUse);
        return newInstance;
    }

    private void DevToolsProtocolEventHandler(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        Debug.WriteLine($"DevToolsProtocolEventReceived: {e.ParameterObjectAsJson}");
    }

    public event EventHandler<WebPageAsJpegWindowImageSavedEventArgs>? ImageSaved;

    [BlockingCommand]
    public async Task SaveCurrentPageAsJpeg()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (JpgScreenshotFunction == null)
        {
            await StatusContext.ToastError("Screenshot function not available...");
            return;
        }

        var screenshotResult = await JpgScreenshotFunction();

        if (screenshotResult.IsT1)
        {
            await StatusContext.ToastError(screenshotResult.AsT1.Value);
            return;
        }

        var newFile = await WebViewToJpg.SaveByteArrayAsJpg(screenshotResult.AsT0.Value, string.Empty, StatusContext);

        if (!string.IsNullOrWhiteSpace(newFile))
            ImageSaved?.Invoke(this, new WebPageAsJpegWindowImageSavedEventArgs(newFile));
    }

    private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var binding = UrlTextBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();
        }
    }
}