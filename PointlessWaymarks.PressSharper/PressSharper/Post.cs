namespace PointlessWaymarks.PressSharper;

public class Post
{
    public Post()
    {
        Categories = Enumerable.Empty<Category>().ToList();
        Tags = Enumerable.Empty<Tag>().ToList();
    }

    public Author? Author { get; init; }
    public string Body { get; init; } = string.Empty;
    public List<Category> Categories { get; }
    public string Excerpt { get; init; } = string.Empty;
    public Attachment? FeaturedImage { get; set; }
    public DateTime PublishDate { get; init; }
    public string Slug { get; init; } = string.Empty;
    public List<Tag> Tags { get; }
    public string Title { get; init; } = string.Empty;
}