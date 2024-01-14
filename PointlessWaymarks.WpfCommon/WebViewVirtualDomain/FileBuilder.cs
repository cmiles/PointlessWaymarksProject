namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

public class FileBuilder
{
    public List<string> Copy { get; set; } = new();
    public List<(string filename, string body)> Create { get; set; } = new();

    public static ToWebViewRequest CreateRequest(List<string> copy, List<(string filename, string body)> create)
    {
        return new ToWebViewRequest(new FileBuilder { Copy = copy, Create = create });
    }
}