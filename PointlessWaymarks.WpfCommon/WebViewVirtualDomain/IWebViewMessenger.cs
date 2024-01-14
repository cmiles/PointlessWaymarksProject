using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

public interface IWebViewMessenger
{
    /// <summary>
    ///     This Queue will be used by the Web View behavior - the implementing class should
    ///     initialize the OneAtATimeWorkQueue and enqueue Json to it - but SHOULD NOT start,
    ///     stop or assign a processor! This unintuitive interface is basically driven by the
    ///     the WebView's async initialization which means the implementer might send Json messages
    ///     well before the WebView and Xaml Bindings are initialized and able to accept
    ///     messages.
    /// </summary>
    WorkQueue<ToWebViewRequest> ToWebView { get; set; }

    void FromWebView(object? o, MessageFromWebView args);
}