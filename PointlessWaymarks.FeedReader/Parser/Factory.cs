namespace PointlessWaymarks.FeedReader.Parser;

internal static class Factory
{
    public static AbstractXmlFeedParser GetParser(FeedType feedType)
    {
        return feedType switch
        {
            FeedType.Atom => new AtomParser(),
            FeedType.Rss_0_91 => new Rss091Parser(),
            FeedType.Rss_0_92 => new Rss092Parser(),
            FeedType.Rss_1_0 => new Rss10Parser(),
            FeedType.MediaRss => new MediaRssParser(),
            _ => new Rss20Parser()
        };
    }
}