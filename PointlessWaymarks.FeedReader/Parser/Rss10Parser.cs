using System.Xml.Linq;
using PointlessWaymarks.FeedReader.Feeds._1._0;
using PointlessWaymarks.FeedReader.Feeds.Base;

namespace PointlessWaymarks.FeedReader.Parser;

internal class Rss10Parser : AbstractXmlFeedParser
{
    public override BaseFeed Parse(string feedXml, XDocument feedDoc)
    {
        var rdf = feedDoc.Root;
        var channel = rdf.GetElement("channel");
        var feed = new Rss10Feed(feedXml, channel);
        return feed;
    }
}