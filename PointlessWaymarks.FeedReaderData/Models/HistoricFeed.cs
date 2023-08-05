using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.FeedReaderData.Models;

[NotifyPropertyChanged]
public class HistoricFeed
{
    public DateTime CreatedOn { get; set; } = DateTime.Now;
    public DateTime? FeedLastUpdatedDate { get; set; }
    public int Id { get; set; }
    public DateTime? LastSuccessfulUpdate { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public required Guid PersistentId { get; set; }
    public string Tags { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}