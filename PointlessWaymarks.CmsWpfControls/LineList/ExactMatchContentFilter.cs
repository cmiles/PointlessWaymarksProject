using DataGridExtensions;

namespace PointlessWaymarks.CmsWpfControls.LineList;

/// <summary>
///     An exact match content filter for the DataGridExtensions - https://github.com/dotnet/DataGridExtensions - this
///     can be useful especially for a quick filter on a numeric type where you want '1' to match '1' not '1','10','11',
///     '12', etc.
/// </summary>
public class ExactMatchContentFilter : IContentFilter
{
    private readonly string _content;
    private readonly StringComparison _stringComparison;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExactMatchContentFilter" /> class.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="stringComparison">The string comparison.</param>
    public ExactMatchContentFilter(string content, StringComparison stringComparison)
    {
        _content = content;
        _stringComparison = stringComparison;
    }

    /// <summary>
    ///     Determines whether the specified value matches the condition of this filter.
    /// </summary>
    /// <param name="value">The content.</param>
    /// <returns>
    ///     <c>true</c> if the specified value matches the condition; otherwise, <c>false</c>.
    /// </returns>
    public bool IsMatch(object? value)
    {
        return value != null &&
               // ReSharper disable once ConstantConditionalAccessQualifier => net5.0 
               (value.ToString() ?? string.Empty).Equals(_content, _stringComparison);
    }
}