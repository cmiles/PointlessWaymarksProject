using System.Xml.Linq;

namespace PointlessWaymarks.FeedReader.Feeds.Base;

/// <summary>
/// Enclosure object of Rss 2.0 according to specification: https://validator.w3.org/feed/docs/rss2.html
/// </summary>
public class FeedItemEnclosure
{
    /// <summary>
    /// The "url" element
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The "length" element as int
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// The "type" element
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedItemEnclosure"/> class.
    /// default constructor (for serialization)
    /// </summary>
    public FeedItemEnclosure()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedItemEnclosure"/> class.
    /// Reads a rss feed item enclosure based on the xml given in element
    /// </summary>
    /// <param name="element">enclosure element as xml</param>
    public FeedItemEnclosure(XElement? element)
    {
        Url = element.GetAttributeValue("url");
        Length = Helpers.TryParseInt(element.GetAttributeValue("length"));
        MediaType = element.GetAttributeValue("type");
    }
}