namespace PointlessWaymarks.RssReaderData.Models;

public class RssFeed
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public Guid PersistentId { get; set; }
    public string Tags { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}