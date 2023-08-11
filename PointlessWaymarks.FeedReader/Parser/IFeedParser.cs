using PointlessWaymarks.FeedReader.Feeds.Base;

namespace PointlessWaymarks.FeedReader.Parser;

internal interface IFeedParser
{
    BaseFeed Parse(string feedXml);
}