using System.Xml.Linq;
using PointlessWaymarks.FeedReader.Feeds._2._0;
using PointlessWaymarks.FeedReader.Feeds.Base;

namespace PointlessWaymarks.FeedReader.Parser;

internal class Rss20Parser : AbstractXmlFeedParser
{
    public override BaseFeed Parse(string feedXml, XDocument feedDoc)
    {
        var rss = feedDoc.Root;
        var channel = rss.GetElement("channel");
        var feed = new Rss20Feed(feedXml, channel);
        return feed;
    }
}