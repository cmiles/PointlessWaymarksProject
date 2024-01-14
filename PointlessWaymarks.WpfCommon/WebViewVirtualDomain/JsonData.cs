namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

public class JsonData
{
    public string Json { get; set; } = string.Empty;

    public static ToWebViewRequest CreateRequest(string json)
    {
        return new ToWebViewRequest(new JsonData { Json = json });
    }
}