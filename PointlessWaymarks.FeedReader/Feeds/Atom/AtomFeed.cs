using System.Xml.Linq;
using PointlessWaymarks.FeedReader.Feeds.Base;

namespace PointlessWaymarks.FeedReader.Feeds.Atom;

/// <summary>
/// Atom 1.0 feed object according to specification: https://validator.w3.org/feed/docs/atom.html
/// </summary>
public class AtomFeed : BaseFeed
{
    /// <summary>
    /// The "author" element
    /// </summary>
    public AtomPerson? Author { get; set; }

    /// <summary>
    /// All "category" elements
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// The "contributor" element
    /// </summary>
    public AtomPerson? Contributor { get; set; }

    /// <summary>
    /// The "generator" element
    /// </summary>
    public string? Generator { get; set; }

    /// <summary>
    /// The "icon" element
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// The "id" element
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The "logo" element
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// The "rights" element
    /// </summary>
    public string? Rights { get; set; }

    /// <summary>
    /// The "subtitle" element
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// The "updated" element as string
    /// </summary>
    public string? UpdatedDateString { get; set; }

    /// <summary>
    /// The "updated" element as DateTime. Null if parsing failed of updatedDate is empty.
    /// </summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// All "link" elements
    /// </summary>
    public List<AtomLink> Links { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AtomFeed"/> class.
    /// default constructor (for serialization)
    /// </summary>
    public AtomFeed()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AtomFeed"/> class.
    /// Reads an atom feed based on the xml given in channel
    /// </summary>
    /// <param name="feedXml">the entire feed xml as string</param>
    /// <param name="feed">the feed element in the xml as XElement</param>
    public AtomFeed(string feedXml, XElement? feed)
        : base(feedXml, feed)
    {
        Link = feed.GetElement("link")?.Attribute("href")?.Value;

        Author = new AtomPerson(feed.GetElement("author"));

        var categories = feed.GetElements("category");
        Categories =
            categories?.Select(x => x.GetValue()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList() ??
            new List<string>();

        Contributor = new AtomPerson(feed.GetElement("contributor"));
        Generator = feed.GetValue("generator");
        Icon = feed.GetValue("icon");
        Id = feed.GetValue("id");
        Logo = feed.GetValue("logo");
        Rights = feed.GetValue("rights");
        Subtitle = feed.GetValue("subtitle");

        Links = feed.GetElements("link")?.Where(x => x != null).Select(x => new AtomLink(x!)).ToList() ?? new List<AtomLink>();

        UpdatedDateString = feed.GetValue("updated");
        UpdatedDate = Helpers.TryParseDateTime(UpdatedDateString);

        var items = feed.GetElements("entry");

        if (items == null) return;

        foreach (var item in items)
        {
            Items.Add(new AtomFeedItem(item));
        }
    }

    /// <summary>
    /// Creates the base <see cref="Feed"/> element out of this feed.
    /// </summary>
    /// <returns>feed</returns>
    public override Feed ToFeed()
    {
        var f = new Feed(this)
        {
            Copyright = Rights,
            Description = null,
            ImageUrl = Icon,
            Language = null,
            LastUpdatedDate = UpdatedDate,
            LastUpdatedDateString = UpdatedDateString,
            Type = FeedType.Atom
        };
        return f;
    }
}