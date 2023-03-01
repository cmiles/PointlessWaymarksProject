namespace PointlessWaymarks.PressSharper;

public class Page
{
    public Author? Author { get; init; }
    public string Body { get; init; } = string.Empty;
    public int Id { get; init; }
    public int? ParentId { get; init; }
    public DateTime PublishDate { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}