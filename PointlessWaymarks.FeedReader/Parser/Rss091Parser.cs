using System.Xml.Linq;
using PointlessWaymarks.FeedReader.Feeds._0._91;
using PointlessWaymarks.FeedReader.Feeds.Base;

namespace PointlessWaymarks.FeedReader.Parser;

internal class Rss091Parser : AbstractXmlFeedParser
{
    public override BaseFeed Parse(string feedXml, XDocument feedDoc)
    {
        var rss = feedDoc.Root;
        var channel = rss.GetElement("channel");
        var feed = new Rss091Feed(feedXml, channel);
        return feed;
    }
}