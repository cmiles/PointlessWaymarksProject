namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

public class NavigateTo
{
    public string Url { get; set; } = string.Empty;
    public bool WaitForScriptFinished { get; set; }

    public static ToWebViewRequest CreateRequest(string navigateTo, bool waitForScriptFinished = false)
    {
        return new ToWebViewRequest(new NavigateTo { Url = navigateTo, WaitForScriptFinished = waitForScriptFinished });
    }
}