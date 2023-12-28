using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.WpfCommon.WpfHtml;

public interface IWebViewMessenger
{
    OneAtATimeWorkQueue<WebViewMessage> JsonToWebView { get; set; }
    void JsonFromWebView(object? o, WebViewMessage args);
}