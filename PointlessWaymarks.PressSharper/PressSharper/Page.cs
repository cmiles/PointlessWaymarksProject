namespace PointlessWaymarks.PressSharper
{
    public class Page
    {
        public Author Author { get; set; }
        public string Body { get; set; }
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public DateTime PublishDate { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
    }
}