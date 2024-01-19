namespace PointlessWaymarks.PressSharper;

public class Post
{
    public Author? Author { get; init; }
    public string Body { get; init; } = string.Empty;
    public List<Category> Categories { get; } = Enumerable.Empty<Category>().ToList();
    public string Excerpt { get; init; } = string.Empty;
    public Attachment? FeaturedImage { get; set; }
    public DateTime? PublishDate { get; init; }
    public string Slug { get; init; } = string.Empty;
    public List<Tag> Tags { get; } = Enumerable.Empty<Tag>().ToList();
    public string Title { get; init; } = string.Empty;
}