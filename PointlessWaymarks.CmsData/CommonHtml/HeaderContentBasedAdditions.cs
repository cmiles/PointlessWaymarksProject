using System.Reflection;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

/// <summary>
///     Static Methods to check for Header Additions from all IContentBasedHeaderAdditions in
///     the Assembly.
/// </summary>
public static class HeaderContentBasedAdditions
{
    public static string HeaderAdditions(List<IContentCommon> content)
    {
        var headers = HeaderIncludes();

        var headerList = new List<string>();

        foreach (var loopHeader in headers)
            if (content.Any(x => loopHeader.IsNeeded(x)))
                headerList.Add(loopHeader.HeaderAdditions());

        return string.Join(Environment.NewLine, headerList.Where(string.IsNullOrWhiteSpace).ToList());
    }

    public static string HeaderAdditions(IContentCommon content)
    {
        var headers = HeaderIncludes();

        var headerList = new List<string>();

        foreach (var loopHeader in headers) headerList.Add(loopHeader.HeaderAdditions(content));

        return string.Join(Environment.NewLine, headerList.Where(string.IsNullOrWhiteSpace).ToList());
    }

    public static string HeaderAdditions(params string?[] stringContent)
    {
        var headers = HeaderIncludes();

        var headerList = new List<string>();

        foreach (var loopHeader in headers) headerList.Add(loopHeader.HeaderAdditions(stringContent));

        return string.Join(Environment.NewLine, headerList.Where(string.IsNullOrWhiteSpace).ToList());
    }

    public static List<IHeaderContentBasedAdditions> HeaderIncludes()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => typeof(IHeaderContentBasedAdditions).IsAssignableFrom(type))
            .Where(type =>
                type is { IsAbstract: false, IsGenericType: false } &&
                type.GetConstructor(Type.EmptyTypes) != null)
            .Select(type => Activator.CreateInstance(type) as IHeaderContentBasedAdditions)
            .Where(x => x is not null)
            .Cast<IHeaderContentBasedAdditions>()
            .ToList();
    }
}