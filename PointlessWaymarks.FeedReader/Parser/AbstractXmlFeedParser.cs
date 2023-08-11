using System.Xml.Linq;
using PointlessWaymarks.FeedReader.Feeds.Base;

namespace PointlessWaymarks.FeedReader.Parser;

internal abstract class AbstractXmlFeedParser : IFeedParser
{
    public BaseFeed Parse(string feedXml)
    {
        var feedDoc = XDocument.Parse(feedXml);

        return Parse(feedXml, feedDoc);
    }

    public abstract BaseFeed Parse(string feedXml, XDocument feedDoc);
}