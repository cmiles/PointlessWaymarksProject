using DataGridExtensions;

namespace PointlessWaymarks.CmsWpfControls.LineList;

/// <summary>
///     Exact Match Filter Factory
/// </summary>
public class ExactMatchContentFilterFactory : IContentFilterFactory
{
    /// <summary>
    ///     The default instance.
    /// </summary>
    public static readonly IContentFilterFactory Default = new ExactMatchContentFilterFactory();

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExactMatchContentFilterFactory" /> class.
    /// </summary>
    public ExactMatchContentFilterFactory()
        : this(StringComparison.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExactMatchContentFilterFactory" /> class.
    /// </summary>
    /// <param name="stringComparison">The string comparison to use.</param>
    public ExactMatchContentFilterFactory(StringComparison stringComparison)
    {
        StringComparison = stringComparison;
    }

    /// <summary>
    ///     Gets or sets the string comparison.
    /// </summary>
    public StringComparison StringComparison { get; set; }

    #region IFilterFactory Members

    /// <summary>
    ///     Creates the content filter for the specified content.
    /// </summary>
    /// <param name="content">The content to create the filter for.</param>
    /// <returns>
    ///     The new filter.
    /// </returns>
    public IContentFilter Create(object? content)
    {
        return new ExactMatchContentFilter(content?.ToString() ?? string.Empty, StringComparison);
    }

    #endregion
}