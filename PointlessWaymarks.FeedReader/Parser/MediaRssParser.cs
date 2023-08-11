using System.Xml.Linq;
using PointlessWaymarks.FeedReader.Feeds.Base;
using PointlessWaymarks.FeedReader.Feeds.MediaRSS;

namespace PointlessWaymarks.FeedReader.Parser;

internal class MediaRssParser : AbstractXmlFeedParser
{
    public override BaseFeed Parse(string feedXml, XDocument feedDoc)
    {
        var rss = feedDoc.Root;
        var channel = rss.GetElement("channel");
        var feed = new MediaRssFeed(feedXml, channel);
        return feed;
    }
}