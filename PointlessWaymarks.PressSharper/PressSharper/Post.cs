using System;
using System.Collections.Generic;
using System.Linq;

namespace PointlessWaymarks.PressSharper
{
    public class Post
    {
        public Post()
        {
            Categories = Enumerable.Empty<Category>().ToList();
            Tags = Enumerable.Empty<Tag>().ToList();
        }

        public Author Author { get; set; }
        public string Body { get; set; }
        public List<Category> Categories { get; set; }
        public string Excerpt { get; set; }
        public Attachment FeaturedImage { get; set; }
        public DateTime PublishDate { get; set; }
        public string Slug { get; set; }
        public List<Tag> Tags { get; set; }
        public string Title { get; set; }
    }
}