using System.Xml.Linq;
using PointlessWaymarks.FeedReader.Feeds.Base;

namespace PointlessWaymarks.FeedReader.Feeds._0._91;

/// <summary>
/// Rss 0.91 Feed Item according to specification: http://www.rssboard.org/rss-0-9-1-netscape#image
/// </summary>
public class Rss091FeedItem : BaseFeedItem
{
    /// <summary>
    /// The "description" field
    /// </summary>
    public string? Description { get; set; } // description

    /// <summary>
    /// The "pubDate" field
    /// </summary>
    public string? PublishingDateString { get; set; }

    /// <summary>
    /// The "pubDate" field as DateTime. Null if parsing failed or pubDate is empty.
    /// </summary>
    public DateTime? PublishingDate { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rss091FeedItem"/> class.
    /// default constructor (for serialization)
    /// </summary>
    public Rss091FeedItem()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rss091FeedItem"/> class.
    /// Creates this object based on the xml in the XElement parameter.
    /// </summary>
    /// <param name="item">feed item as xml</param>
    public Rss091FeedItem(XElement? item)
        : base(item)
    {
        Description = item.GetValue("description");
        PublishingDateString = item.GetValue("pubDate");
        PublishingDate = Helpers.TryParseDateTime(PublishingDateString);
    }

    internal override FeedItem ToFeedItem()
    {
        var fi = new FeedItem(this)
        {
            Description = Description,
            PublishingDate = PublishingDate,
            PublishingDateString = PublishingDateString,
            Id = Link
        };
        return fi;
    }
}