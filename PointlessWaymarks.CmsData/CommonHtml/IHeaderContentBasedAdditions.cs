using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

/// <summary>
///     Interface to include snippets in the Header of generated HTML based on Content. See the static
///     ContentHeaders class for an easy way to use these.
/// </summary>
public interface IHeaderContentBasedAdditions
{
    /// <summary>
    ///     Returns the needed Header Includes based on an IContentCommon. IContentCommon
    ///     may or may not contain all the fields for a piece of content that you want to
    ///     check - be sure to look at the IContentCommon interface.
    /// </summary>
    /// <param name="content"></param>
    /// <returns>HeaderAdditions() or empty string</returns>
    string HeaderAdditions(IContentCommon content);

    /// <summary>
    ///     Return header includes based on the submitted strings.
    /// </summary>
    /// <param name="stringContent"></param>
    /// <returns>HeaderAdditions() or empty string</returns>
    string HeaderAdditions(params string?[] stringContent);

    string HeaderAdditions();
    bool IsNeeded(params string?[] stringContent);
    bool IsNeeded(IContentCommon content);
}