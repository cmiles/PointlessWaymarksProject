namespace PointlessWaymarks.PressSharper;

public class Page
{
    public Author Author { get; init; }
    public string Body { get; init; }
    public int Id { get; init; }
    public int? ParentId { get; init; }
    public DateTime PublishDate { get; init; }
    public string Slug { get; init; }
    public string Title { get; init; }
}