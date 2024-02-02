using System.Runtime.CompilerServices;

namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

public class NavigateTo
{
    public string RequestTag { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool WaitForScriptFinished { get; set; }


    public static ToWebViewRequest CreateRequest(string navigateTo, bool waitForScriptFinished = false,
        [CallerMemberName] string requestTag = "None")
    {
        return new ToWebViewRequest(new NavigateTo
            { Url = navigateTo, WaitForScriptFinished = waitForScriptFinished, RequestTag = requestTag });
    }
}