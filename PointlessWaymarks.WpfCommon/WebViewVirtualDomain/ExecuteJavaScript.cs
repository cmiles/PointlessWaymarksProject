using System.Runtime.CompilerServices;

namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

public class ExecuteJavaScript
{
    public string RequestTag { get; set; } = string.Empty;
    public string JavaScriptToExecute { get; set; } = string.Empty;
    public bool WaitForScriptFinished { get; set; }


    public static ToWebViewRequest CreateRequest(string toExecute, bool waitForScriptFinished = false,
        [CallerMemberName] string requestTag = "None")
    {
        return new ToWebViewRequest(new ExecuteJavaScript
            { JavaScriptToExecute = toExecute, WaitForScriptFinished = waitForScriptFinished, RequestTag = requestTag });
    }
}