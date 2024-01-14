namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

public class MessageFromWebView(string message) : EventArgs
{
    public string? Message { get; } = message;
}