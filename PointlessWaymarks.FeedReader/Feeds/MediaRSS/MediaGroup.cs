using System.Xml.Linq;

namespace PointlessWaymarks.FeedReader.Feeds.MediaRSS;

/// <summary>
/// A collection of media that are effectively the same content, yet different representations. For isntance: the same song recorded in both WAV and MP3 format.
/// </summary>
public class MediaGroup
{
    /// <summary>
    /// Gets the underlying XElement in order to allow reading properties that are not available in the class itself
    /// </summary>
    public XElement Element { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaGroup"/> class.
    /// Reads a rss media group item enclosure based on the xml given in element
    /// </summary>
    /// <param name="element">enclosure element as xml</param>
    public MediaGroup(XElement element)
    {
        Element = element;
        var media = element.GetElements("media", "content");
        Media = media?.Where(x => x != null).Select(x => new Media(x!)).ToList() ?? new List<Media>();
    }

    /// <summary>
    /// Media object
    /// </summary>
    public List<Media> Media { get; set; }
}