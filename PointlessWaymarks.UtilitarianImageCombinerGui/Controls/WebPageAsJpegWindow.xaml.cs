using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using SkiaSharp;

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

    public StatusControlContext StatusContext { get; set; }

    public string UserUrl { get; set; }

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
        var progress = StatusContext.ProgressTracker();

        await ThreadSwitcher.ResumeForegroundAsync();

        var maxImageHeightString = await WebContentWebView.CoreWebView2.ExecuteScriptAsync(
            """
            canvas = document.createElement('canvas');
            gl = canvas.getContext('webgl');
            gl.getParameter(gl.MAX_TEXTURE_SIZE)
            """);
        var maxImageHeight = int.Parse(maxImageHeightString);

        var documentWidthString = await WebContentWebView.CoreWebView2.ExecuteScriptAsync(@"document.body.scrollWidth");
        var documentWidth = int.Parse(documentWidthString);
        var documentHeightString =
            await WebContentWebView.CoreWebView2.ExecuteScriptAsync(@"document.body.scrollHeight");
        var documentHeight = int.Parse(documentHeightString);

        var chunks = (int)Math.Ceiling((double)documentHeight / maxImageHeight);
        var imageBytesList = new List<byte[]>();

        for (var i = 0; i < chunks; i++)
        {
            var clip = new
            {
                x = 0,
                y = i * maxImageHeight,
                width = documentWidth,
                height = Math.Min(maxImageHeight, documentHeight - i * maxImageHeight),
                scale = 1
            };

            var settings = new
            {
                format = "jpeg",
                clip,
                fromSurface = true,
                captureBeyondViewport = true
            };

            var screenshotParamsJson = JsonSerializer.Serialize(settings);

            await ThreadSwitcher.ResumeForegroundAsync();

            WebContentWebView.CoreWebView2.GetDevToolsProtocolEventReceiver("Log.entryAdded")
                .DevToolsProtocolEventReceived += DevToolsProtocolEventHandler;
            await WebContentWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Log.enable", "{}");

            var imageResultsJson =
                await WebContentWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot",
                    screenshotParamsJson);

            var imageResults = string.Empty;

            using (var doc = JsonDocument.Parse(imageResultsJson))
            {
                if (doc.RootElement.TryGetProperty("data", out var dataElement))
                {
                    imageResults = dataElement.GetString() ?? string.Empty;

                    if (string.IsNullOrEmpty(imageResults))
                    {
                        await StatusContext.ToastError("Failed to capture screenshot - empty data.");
                        return;
                    }
                }
                else
                {
                    await StatusContext.ToastError("Failed to capture screenshot - data json not found.");
                    return;
                }
            }

            progress.Report($"Converting to byte[] - Base64 length {imageResults.Length} ");
            var imageBytes = Convert.FromBase64String(imageResults);
            imageBytesList.Add(imageBytes);
        }

        progress.Report("Combining image chunks");

        using var finalImage = new SKBitmap(documentWidth, documentHeight);
        using var canvas = new SKCanvas(finalImage);
        var currentHeight = 0;

        foreach (var imageBytes in imageBytesList)
        {
            using var image = SKBitmap.Decode(imageBytes);
            canvas.DrawBitmap(image, new SKPoint(0, currentHeight));
            currentHeight += image.Height;
        }

        using var imageStream = new MemoryStream();
        using var finalImageEncoded = SKImage.FromBitmap(finalImage);
        finalImageEncoded.Encode(SKEncodedImageFormat.Jpeg, 100).SaveTo(imageStream);
        var finalImageBytes = imageStream.ToArray();

        progress.Report("Getting User Save Information");

        var saveDialog = new VistaSaveFileDialog { Filter = "jpg files (*.jpg;*.jpeg)|*.jpg;*.jpeg" };

        var currentSettings = ImageCombinerGuiSettingTools.ReadSettings();
        if (!string.IsNullOrWhiteSpace(currentSettings.SaveToDirectory))
            saveDialog.FileName = $"{currentSettings.SaveToDirectory}\\";

        if (!saveDialog.ShowDialog() ?? true) return;

        var newFilename = saveDialog.FileName;

        if (!(Path.GetExtension(newFilename).Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
              Path.GetExtension(newFilename).Equals(".jpeg", StringComparison.OrdinalIgnoreCase)))
            newFilename += ".jpg";

        await ThreadSwitcher.ResumeBackgroundAsync();

        progress.Report($"Writing {finalImageBytes.Length} image bytes");

        await File.WriteAllBytesAsync(newFilename, finalImageBytes);

        await StatusContext.ToastSuccess($"Screenshot saved to {newFilename}");

        ImageSaved?.Invoke(this, new WebPageAsJpegWindowImageSavedEventArgs(newFilename));
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