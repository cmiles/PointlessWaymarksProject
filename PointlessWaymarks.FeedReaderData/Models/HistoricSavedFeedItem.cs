using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.FeedReaderData.Models;

[NotifyPropertyChanged]
public class HistoricSavedFeedItem
{
    public string? Author { get; set; } = string.Empty;
    public string? Content { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? Description { get; set; } = string.Empty;
    public string FeedItemId { get; set; } = string.Empty;
    public Guid FeedItemPersistentId { get; set; }
    public Guid FeedPersistentId { get; set; }
    public string? FeedTitle { get; set; }
    public int Id { get; set; }
    public string? Link { get; set; } = string.Empty;
    public Guid PersistentId { get; set; } = Guid.NewGuid();
    public DateTime? PublishingDate { get; set; }
    public string? Title { get; set; } = string.Empty;
}