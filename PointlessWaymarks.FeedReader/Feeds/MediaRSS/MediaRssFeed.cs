using System.Globalization;
using System.Xml.Linq;
using PointlessWaymarks.FeedReader.Feeds.Base;

namespace PointlessWaymarks.FeedReader.Feeds.MediaRSS;

/// <summary>
/// Media RSS 2.0 feed according to specification: http://www.rssboard.org/media-rss
/// </summary>
public class MediaRssFeed : BaseFeed
{
    /// <summary>
    /// The "description" element
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The "language" element
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// The "copyright" element
    /// </summary>
    public string? Copyright { get; set; }

    /// <summary>
    /// The "docs" element
    /// </summary>
    public string? Docs { get; set; }

    /// <summary>
    /// The "image" element
    /// </summary>
    public FeedImage? Image { get; set; }

    /// <summary>
    /// The "lastBuildDate" element as string
    /// </summary>
    public string? LastBuildDateString { get; set; }

    /// <summary>
    /// The "lastBuildDate" element as DateTime. Null if parsing failed of lastBuildDate is empty.
    /// </summary>
    public DateTime? LastBuildDate { get; set; }

    /// <summary>
    /// The "managingEditor" element
    /// </summary>
    public string? ManagingEditor { get; set; }

    /// <summary>
    /// The "pubDate" field
    /// </summary>
    public string? PublishingDateString { get; set; }

    /// <summary>
    /// The "pubDate" field as DateTime. Null if parsing failed or pubDate is empty.
    /// </summary>
    public DateTime? PublishingDate { get; set; }

    /// <summary>
    /// The "webMaster" field
    /// </summary>
    public string? WebMaster { get; set; }

    /// <summary>
    /// All "category" elements
    /// </summary>
    public List<string> Categories { get; set; } = [];

    /// <summary>
    /// The "generator" element
    /// </summary>
    public string? Generator { get; set; }

    /// <summary>
    /// The "cloud" element
    /// </summary>
    public FeedCloud? Cloud { get; set; }

    /// <summary>
    /// The time to life "ttl" element
    /// </summary>
    public string? TTL { get; set; }

    /// <summary>
    /// All "day" elements in "skipDays"
    /// </summary>
    public List<string> SkipDays { get; set; } = [];

    /// <summary>
    /// All "hour" elements in "skipHours"
    /// </summary>
    public List<string> SkipHours { get; set; } = [];

    /// <summary>
    /// The "textInput" element
    /// </summary>
    public FeedTextInput? TextInput { get; set; }

    /// <summary>
    /// All elements starting with "sy:"
    /// </summary>
    public Syndication? Sy { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaRssFeed"/> class.
    /// default constructor (for serialization)
    /// </summary>
    public MediaRssFeed()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaRssFeed"/> class.
    /// Reads a Media Rss feed based on the xml given in channel
    /// </summary>
    /// <param name="feedXml">the entire feed xml as string</param>
    /// <param name="channel">the "channel" element in the xml as XElement</param>
    public MediaRssFeed(string feedXml, XElement? channel)
        : base(feedXml, channel)
    {
        Description = channel.GetValue("description");
        Language = channel.GetValue("language");
        Copyright = channel.GetValue("copyright");
        ManagingEditor = channel.GetValue("managingEditor");
        WebMaster = channel.GetValue("webMaster");
        Docs = channel.GetValue("docs");
        PublishingDateString = channel.GetValue("pubDate");
        LastBuildDateString = channel.GetValue("lastBuildDate");
        ParseDates(Language, PublishingDateString, LastBuildDateString);

        var categories = channel.GetElements("category");
        Categories =
            categories?.Select(x => x.GetValue()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList() ??
            [];

        Sy = new Syndication(channel);
        Generator = channel.GetValue("generator");
        TTL = channel.GetValue("ttl");
        Image = new MediaRssFeedImage(channel.GetElement("image"));
        Cloud = new FeedCloud(channel.GetElement("cloud"));
        TextInput = new FeedTextInput(channel.GetElement("textinput"));

        var skipHours = channel.GetElement("skipHours");
        if (skipHours != null)
            SkipHours = skipHours.GetElements("hour")?.Select(x => x.GetValue())
                .Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList() ?? [];

        var skipDays = channel.GetElement("skipDays");
        if (skipDays != null)
            SkipDays = skipDays.GetElements("day")?.Select(x => x.GetValue()).Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>().ToList() ?? [];

        var items = channel.GetElements("item");

        if(items == null)
            return;

        foreach (var item in items)
        {
            Items.Add(new MediaRssFeedItem(item));
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
            Copyright = Copyright,
            Description = Description,
            ImageUrl = Image?.Url,
            Language = Language,
            LastUpdatedDate = LastBuildDate,
            LastUpdatedDateString = LastBuildDateString,
            Type = FeedType.MediaRss
        };
        return f;
    }

    /// <summary>
    /// Sets the PublishingDate and LastBuildDate. If parsing fails, then it checks if the language
    /// is set and tries to parse the date based on the culture of the language.
    /// </summary>
    /// <param name="language">language of the feed</param>
    /// <param name="publishingDate">publishing date as string</param>
    /// <param name="lastBuildDate">last build date as string</param>
    private void ParseDates(string? language, string? publishingDate, string? lastBuildDate)
    {
        PublishingDate = Helpers.TryParseDateTime(publishingDate);
        LastBuildDate = Helpers.TryParseDateTime(lastBuildDate);

        // check if language is set - if so, check if dates could be parsed or try to parse it with culture of the language
        if (string.IsNullOrWhiteSpace(language))
            return;

        // if publishingDateString is set but PublishingDate is null - try to parse with culture of "Language" property
        var parseLocalizedPublishingDate = PublishingDate == null && !string.IsNullOrWhiteSpace(PublishingDateString);

        // if LastBuildDateString is set but LastBuildDate is null - try to parse with culture of "Language" property
        var parseLocalizedLastBuildDate = LastBuildDate == null && !string.IsNullOrWhiteSpace(LastBuildDateString);

        // if both dates are set - return
        if (!parseLocalizedPublishingDate && !parseLocalizedLastBuildDate)
            return;

        // dates are set, but one of them couldn't be parsed - so try again with the culture set to the language
        CultureInfo? culture;
        try
        {
            culture = new CultureInfo(Language ?? "en-US");
        }
        catch (CultureNotFoundException)
        {
            // should be replace by a try parse or by getting all cultures and selecting the culture
            // out of the collection. That's unfortunately not available in .net standard 1.3 for now
            // this case should never happen, but in some rare cases it does - so catching the exception
            // is acceptable in that case.
            return; // culture couldn't be found, return as it was already tried to parse with default values
        }

        if (parseLocalizedPublishingDate)
        {
            PublishingDate = Helpers.TryParseDateTime(PublishingDateString, culture);
        }

        if (parseLocalizedLastBuildDate)
        {
            LastBuildDate = Helpers.TryParseDateTime(LastBuildDateString, culture);
        }
    }
}