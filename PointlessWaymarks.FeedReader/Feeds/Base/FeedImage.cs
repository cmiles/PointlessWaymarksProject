using System.Xml.Linq;

namespace PointlessWaymarks.FeedReader.Feeds.Base;

/// <summary>
/// feed image object that is used in feed (rss 0.91, 2.0, atom, ...)
/// </summary>
public class FeedImage
{
    /// <summary>
    /// The "title" element
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The "url" element
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The "link" element
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedImage"/> class.
    /// default constructor (for serialization)
    /// </summary>
    public FeedImage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedImage"/> class.
    /// Reads a feed image based on the xml given in element
    /// </summary>
    /// <param name="element">feed image as xml</param>
    public FeedImage(XElement? element)
    {
        Title = element.GetValue("title");
        Url = element.GetValue("url");
        Link = element.GetValue("link");
    }
}