using System.Diagnostics;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Xaml.Behaviors;

namespace PointlessWaymarks.WpfCommon.WpfHtml;

public class WebViewSelectedTextBehavior : Behavior<WebView2>
{
    public static readonly DependencyProperty WebViewSelectedTextProperty =
        DependencyProperty.Register(nameof(WebViewSelectedText), typeof(string), typeof(WebViewSelectedTextBehavior),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string WebViewSelectedText
    {
        get => (string) GetValue(WebViewSelectedTextProperty);
        set => SetValue(WebViewSelectedTextProperty, value);
    }

    private void MessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var possibleString = e.TryGetWebMessageAsString();
        // ReSharper disable once StringLiteralTypo
        if (possibleString.StartsWith("document.onselectionchange:"))
        {
            possibleString = possibleString.Length <= 27
                ? string.Empty
                : possibleString.Substring(27, possibleString.Length - 27);
            SetValue(WebViewSelectedTextProperty, possibleString);
            Debug.Print(possibleString);
        }
    }

    protected override void OnAttached()
    {
        AssociatedObject.CoreWebView2InitializationCompleted += OnReady;
        AssociatedObject.WebMessageReceived += MessageReceived;
    }

    private void OnReady(object? sender, EventArgs e)
    {
        if (sender is WebView2 { CoreWebView2: { } } webView)
            // ReSharper disable StringLiteralTypo
            webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
document.onselectionchange = () => {
  console.log(`document.onselectionchange:${document.getSelection().toString()}`);
  window.chrome.webview.postMessage(`document.onselectionchange:${document.getSelection().toString()}`);
};
");
        // ReSharper restore StringLiteralTypo
    }
}