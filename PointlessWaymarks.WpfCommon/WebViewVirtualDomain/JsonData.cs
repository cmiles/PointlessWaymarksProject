using System.Runtime.CompilerServices;

namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

public class JsonData
{
    public string Json { get; set; } = string.Empty;
    public string RequestTag { get; set; } = string.Empty;

    public static ToWebViewRequest CreateRequest(string json, [CallerMemberName] string requestTag = "None")
    {
        return new ToWebViewRequest(new JsonData { Json = json, RequestTag = requestTag });
    }
}