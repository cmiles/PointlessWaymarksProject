namespace PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

using OneOf;

public record FileBuilderCreate(string FileName, OneOf<string, byte[]> Content, bool TryToOverwrite = false);