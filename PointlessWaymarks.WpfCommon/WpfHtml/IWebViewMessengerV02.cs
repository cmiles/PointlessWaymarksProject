using OneOf;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.WpfCommon.WpfHtml;

public interface IWebViewMessengerV02
{
    /// <summary>
    ///     This Queue will be used by the Web View behavior - the implementing class should
    ///     initialize the OneAtATimeWorkQueue and enqueue Json to it - but SHOULD NOT start,
    ///     stop or assign a processor! This unintuitive interface is basically driven by the
    ///     the WebView's async initialization which means the implementer might send Json messages
    ///     well before the WebView and Xaml Bindings are initialized and able to accept
    ///     messages.
    /// </summary>
    WorkQueue<WebViewMessageV02> ToWebView { get; set; }

    void JsonFromWebView(object? o, WebViewMessage args);
}

public class WebViewMessageV02(WebViewRequest request) : EventArgs
{
    public WebViewRequest Request { get; set; } = request;
}

[GenerateOneOf]
public partial class WebViewRequest : OneOfBase<WebViewFileBuilder, WebViewNavigation, WebViewJson>
{
}

public class WebViewJson
{
    public string Json { get; set; } = string.Empty;
}

public class WebViewNavigation
{
    public string NavigateTo { get; set; } = string.Empty;
    public bool WaitForScriptFinished { get; set; }
}

public class WebViewFileBuilder
{
    public List<string> Copy { get; set; } = new();
    public List<(string filename, string body)> Create { get; set; } = new();
}