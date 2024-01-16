namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

public class FromWebViewMessage(string message) : EventArgs
{
    public string? Message { get; } = message;
}