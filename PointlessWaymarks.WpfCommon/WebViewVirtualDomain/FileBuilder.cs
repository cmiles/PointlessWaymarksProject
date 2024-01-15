using System.Runtime.CompilerServices;

namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

public class FileBuilder
{
    public List<string> Copy { get; set; } = new();
    public List<(string filename, string body)> Create { get; set; } = new();

    public string RequestTag { get; set; } = string.Empty;
    public bool TryToOverwriteExistingFiles { get; set; }


    public static ToWebViewRequest CreateRequest(List<string> copy,
        List<(string filename, string body)> create, bool tryToOverwriteExistingFiles,
        [CallerMemberName] string requestTag = "None")
    {
        return new ToWebViewRequest(new FileBuilder
        {
            Copy = copy, Create = create, TryToOverwriteExistingFiles = tryToOverwriteExistingFiles,
            RequestTag = requestTag
        });
    }
}