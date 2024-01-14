namespace PointlessWaymarks.WpfCommon.WpfHtml;

public class WebViewMessage : EventArgs
{
    public WebViewMessage(string message)
    {
        Message = message;
    }

    public string? Message { get; }
}