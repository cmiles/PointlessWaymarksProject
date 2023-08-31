using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.FeedReaderData.Models;

[NotifyPropertyChanged]
public class ReaderFeedItem
{
    public string? Author { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? Description { get; set; }
    public string FeedItemId { get; set; } = string.Empty;
    public required Guid FeedPersistentId { get; set; }
    public int Id { get; set; }
    public bool KeepUnread { get; set; }
    public string? Link { get; set; }
    public bool MarkedRead { get; set; }
    public Guid PersistentId { get; set; } = Guid.NewGuid();
    public DateTime? PublishingDate { get; set; }
    public string? Title { get; set; }
}