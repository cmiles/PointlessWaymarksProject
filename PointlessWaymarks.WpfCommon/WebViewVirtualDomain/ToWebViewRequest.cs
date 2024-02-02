using OneOf;

namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

[GenerateOneOf]
public partial class ToWebViewRequest : OneOfBase<FileBuilder, NavigateTo, JsonData, ExecuteJavaScript>
{
}