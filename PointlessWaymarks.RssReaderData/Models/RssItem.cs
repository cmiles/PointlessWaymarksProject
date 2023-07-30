namespace PointlessWaymarks.RssReaderData.Models;

public class RssItem
{
    public DateTime CreatedOn { get; set; }
    public string? FeedAuthor { get; set; } = string.Empty;
    public string? FeedContent { get; set; } = string.Empty;
    public string? FeedDescription { get; set; } = string.Empty;
    public string FeedId { get; set; } = string.Empty;
    public string? FeedLink { get; set; } = string.Empty;
    public DateTime? FeedPublishingDate { get; set; }
    public string? FeedTitle { get; set; } = string.Empty;
    
    public int Id { get; set; }
    public bool KeepUnread { get; set; }
    public bool MarkedRead { get; set; }
    public Guid PersistentId { get; set; } = Guid.NewGuid();
    public required Guid RssFeedPersistentId { get; set; }
}