using System.Xml.Linq;
using PointlessWaymarks.FeedReader.Feeds.Atom;
using PointlessWaymarks.FeedReader.Feeds.Base;

namespace PointlessWaymarks.FeedReader.Parser;

internal class AtomParser : AbstractXmlFeedParser
{
    public override BaseFeed Parse(string feedXml, XDocument feedDoc)
    {
        var feed = new AtomFeed(feedXml, feedDoc.Root);
        return feed;
    }
}