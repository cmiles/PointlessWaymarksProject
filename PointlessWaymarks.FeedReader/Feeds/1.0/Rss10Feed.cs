using System.Xml.Linq;
using PointlessWaymarks.FeedReader.Feeds.Base;

namespace PointlessWaymarks.FeedReader.Feeds._1._0;

/// <summary>
/// Rss 1.0 Feed according to specification: http://web.resource.org/rss/1.0/spec
/// </summary>
public class Rss10Feed : BaseFeed
{
    /// <summary>
    /// The "about" attribute of the element
    /// </summary>
    public string? About { get; set; }

    /// <summary>
    /// All elements starting with "dc:"
    /// </summary>
    public DublinCore? DC { get; set; }

    /// <summary>
    /// All elements starting with "sy:"
    /// </summary>
    public Syndication? Sy { get; set; }

    /// <summary>
    /// The "description" field
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The "image" element
    /// </summary>
    public FeedImage? Image { get; set; }

    /// <summary>
    /// The "textInput" element
    /// </summary>
    public FeedTextInput? TextInput { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rss10Feed"/> class.
    /// default constructor (for serialization)
    /// </summary>
    public Rss10Feed()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rss10Feed"/> class.
    /// Reads a rss 1.0 feed based on the xml given in xelement
    /// </summary>
    /// <param name="feedXml">the entire feed xml as string</param>
    /// <param name="channel">the "channel" element in the xml as XElement</param>
    public Rss10Feed(string feedXml, XElement? channel)
        : base(feedXml, channel)
    {
        About = channel.GetAttribute("rdf:about").GetValue();
        DC = new DublinCore(channel);
        Sy = new Syndication(channel);
        Description = channel.GetValue("description");

        Image = new Rss10FeedImage(channel?.Parent.GetElement("image"));
        TextInput = new Rss10FeedTextInput(channel?.Parent.GetElement("textinput"));

        var items = channel?.Parent?.GetElements("items");

        if (items == null) return;

        foreach (var item in items)
        {
            Items.Add(new Rss10FeedItem(item));
        }
    }

    /// <summary>
    /// Creates the base <see cref="Feed"/> element out of this feed.
    /// </summary>
    /// <returns>feed</returns>
    public override Feed ToFeed()
    {
        var f = new Feed(this);

        if (DC != null)
        {
            f.Copyright = DC.Rights;
            f.Language = DC.Language;
            f.LastUpdatedDate = DC.Date;
            f.LastUpdatedDateString = DC.DateString;
        }

        f.Description = Description;
        f.ImageUrl = Image?.Url;
        f.Type = FeedType.Rss_1_0;

        return f;
    }
}