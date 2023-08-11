using System.Xml.Linq;
using PointlessWaymarks.FeedReader.Feeds.Base;

namespace PointlessWaymarks.FeedReader.Feeds._1._0;

/// <summary>
/// Rss 1.0 Feed Item according to specification: http://web.resource.org/rss/1.0/spec
/// </summary>
public class Rss10FeedItem : BaseFeedItem
{
    /// <summary>
    /// The "about" attribute of the element
    /// </summary>
    public string? About { get; set; }

    /// <summary>
    /// The "description" field
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// All elements starting with "dc:"
    /// </summary>
    public DublinCore? DC { get; set; }

    /// <summary>
    /// All elements starting with "sy:"
    /// </summary>
    public Syndication? Sy { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rss10FeedItem"/> class.
    /// default constructor (for serialization)
    /// </summary>
    public Rss10FeedItem()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rss10FeedItem"/> class.
    /// Reads a rss 1.0 feed item based on the xml given in item
    /// </summary>
    /// <param name="item">feed item as xml</param>
    public Rss10FeedItem(XElement? item)
        : base(item)
    {
        DC = new DublinCore(item);
        Sy = new Syndication(item);

        About = item.GetAttribute("rdf:about").GetValue();
        Description = item.GetValue("description");
    }

    /// <inheritdoc/>
    internal override FeedItem ToFeedItem()
    {
        var f = new FeedItem(this);

        if (DC != null)
        {
            f.Author = DC.Creator;
            f.Content = DC.Description;
            f.PublishingDate = DC.Date;
            f.PublishingDateString = DC.DateString;
        }

        f.Description = Description;
        if (string.IsNullOrEmpty(f.Content))
            f.Content = Description;
        f.Id = Link;

        return f;
    }
}