using System.Runtime.CompilerServices;

namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

public class FileBuilder
{
    public List<FileBuilderCopy> Copy { get; set; } = [];
    public List<FileBuilderCreate> Create { get; set; } = [];
    public string RequestTag { get; set; } = string.Empty;

    public static ToWebViewRequest CreateRequest(List<FileBuilderCopy> copy,
        List<FileBuilderCreate> create,
        [CallerMemberName] string requestTag = "None")
    {
        return new ToWebViewRequest(new FileBuilder
        {
            Copy = copy, Create = create,
            RequestTag = requestTag
        });
    }
}