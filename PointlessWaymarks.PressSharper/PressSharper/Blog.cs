using System.Xml;
using System.Xml.Linq;

namespace PointlessWaymarks.PressSharper
{
    public class Blog
    {
        private static readonly XNamespace ContentNamespace = "http://purl.org/rss/1.0/modules/content/";
        private static readonly XNamespace DublinCoreNamespace = "http://purl.org/dc/elements/1.1/";
        private static readonly XNamespace ExcerptNamespace = "http://wordpress.org/export/1.2/excerpt/";
        private static readonly XNamespace WordPressNamespace = "http://wordpress.org/export/1.2/";

        private XElement _channelElement;

        public Blog(string xml) : this(XDocument.Parse(xml))
        {
        }

        public Blog(XDocument doc)
        {
            Authors = Enumerable.Empty<Author>();
            Attachments = Enumerable.Empty<Attachment>();

            InitializeChannelElement(doc);

            if (_channelElement == null) throw new XmlException("Missing channel element.");

            Initialize();
        }

        public IEnumerable<Attachment> Attachments { get; set; }
        public IEnumerable<Author> Authors { get; set; }
        public string Description { get; set; }

        public string Title { get; set; }

        private Attachment GetAttachmentById(int attachmentId)
        {
            return Attachments.FirstOrDefault(a => a.Id == attachmentId);
        }

        private Author GetAuthorByUsername(string username)
        {
            return Authors.FirstOrDefault(a => a.Username == username);
        }

        private string GetBasicProperty(string elementName)
        {
            var element = _channelElement.Element(elementName);
            if (element == null) throw new XmlException($"Missing {elementName}.");

            return element.Value;
        }

        public IEnumerable<Page> GetPages()
        {
            return _channelElement.Elements("item").Where(e => IsPageItem(e) && IsPublished(e))
                .Select(ParsePageElement);
        }

        public IEnumerable<Post> GetPosts()
        {
            return _channelElement.Elements("item").Where(e => IsPostItem(e) && IsPublished(e))
                .Select(ParsePostElement);
        }

        private void Initialize()
        {
            InitializeTitle();
            InitializeDescription();
            InitializeAuthors();
            InitializeAttachments();
        }

        private void InitializeAttachments()
        {
            Attachments = _channelElement.Elements("item").Where(IsAttachmentItem).Select(ParseAttachmentElement);
        }

        private void InitializeAuthors()
        {
            Authors = _channelElement.Descendants(WordPressNamespace + "author").Select(ParseAuthorElement);
        }

        private void InitializeChannelElement(XDocument document)
        {
            var rssRootElement = document.Root;
            if (rssRootElement == null) throw new XmlException("No document root.");

            _channelElement = rssRootElement.Element("channel");
        }

        private void InitializeDescription()
        {
            Description = GetBasicProperty("description");
        }

        private void InitializeTitle()
        {
            Title = GetBasicProperty("title");
        }

        private static bool IsAttachmentItem(XElement itemElement)
        {
            return itemElement?.Element(WordPressNamespace + "post_type")?.Value == "attachment";
        }

        private static bool IsPageItem(XElement itemElement)
        {
            return itemElement?.Element(WordPressNamespace + "post_type")?.Value == "page";
        }

        private static bool IsPostItem(XElement itemElement)
        {
            return itemElement?.Element(WordPressNamespace + "post_type")?.Value == "post";
        }

        private static bool IsPublished(XElement itemElement)
        {
            return itemElement?.Element(WordPressNamespace + "status")?.Value == "publish";
        }

        private static Attachment ParseAttachmentElement(XElement attachmentElement)
        {
            var attachmentIdElement = attachmentElement.Element(WordPressNamespace + "post_id");
            var attachmentTitleElement = attachmentElement.Element("title");
            var attachmentUrlElement = attachmentElement.Element(WordPressNamespace + "attachment_url");

            if (attachmentIdElement == null || attachmentTitleElement == null || attachmentUrlElement == null)
                throw new XmlException("Unable to parse malformed attachment.");

            var attachment = new Attachment
            {
                Id = int.Parse(attachmentIdElement.Value),
                Title = attachmentTitleElement.Value,
                Url = attachmentUrlElement.Value
            };

            return attachment;
        }

        private static Author ParseAuthorElement(XElement authorElement)
        {
            var authorIdElement = authorElement.Element(WordPressNamespace + "author_id");
            var authorUsernameElement = authorElement.Element(WordPressNamespace + "author_login");
            var authorEmailElement = authorElement.Element(WordPressNamespace + "author_email");
            var authorDisplayNameElement = authorElement.Element(WordPressNamespace + "author_display_name");

            if (authorIdElement == null || authorUsernameElement == null || authorEmailElement == null ||
                authorDisplayNameElement == null) throw new XmlException("Unable to parse malformed author.");

            var author = new Author
            {
                Id = int.Parse(authorIdElement.Value),
                Username = authorUsernameElement.Value,
                Email = authorEmailElement.Value,
                DisplayName = authorDisplayNameElement.Value
            };

            return author;
        }

        private Page ParsePageElement(XElement pageElement)
        {
            var pageIdElement = pageElement.Element(WordPressNamespace + "post_id");
            var pageParentIdElement = pageElement.Element(WordPressNamespace + "post_parent");
            var pageTitleElement = pageElement.Element("title");
            var pageUsernameElement = pageElement.Element(DublinCoreNamespace + "creator");
            var pageBodyElement = pageElement.Element(ContentNamespace + "encoded");
            var pagePublishDateElement = pageElement.Element(WordPressNamespace + "post_date");
            var pageSlugElement = pageElement.Element(WordPressNamespace + "post_name");

            if (pageIdElement == null || pageParentIdElement == null || pageTitleElement == null ||
                pageUsernameElement == null || pageBodyElement == null || pagePublishDateElement == null ||
                pageSlugElement == null)
                throw new XmlException("Unable to parse malformed page.");

            var page = new Page
            {
                Id = int.Parse(pageIdElement.Value),
                ParentId = pageParentIdElement.Value != "0" ? int.Parse(pageParentIdElement.Value) : null,
                Author = GetAuthorByUsername(pageUsernameElement.Value),
                Body = pageBodyElement.Value,
                PublishDate = DateTime.Parse(pagePublishDateElement.Value),
                Slug = pageSlugElement.Value,
                Title = pageTitleElement.Value
            };

            return page;
        }

        private Post ParsePostElement(XElement postElement)
        {
            var postTitleElement = postElement.Element("title");
            var postUsernameElement = postElement.Element(DublinCoreNamespace + "creator");
            var postBodyElement = postElement.Element(ContentNamespace + "encoded");
            var postPublishDateElement = postElement.Element(WordPressNamespace + "post_date");
            var postSlugElement = postElement.Element(WordPressNamespace + "post_name");

            if (postTitleElement == null || postUsernameElement == null || postBodyElement == null ||
                postPublishDateElement == null || postSlugElement == null)
                throw new XmlException("Unable to parse malformed post.");

            var postExcerptElement = postElement.Element(ExcerptNamespace + "encoded");

            var post = new Post
            {
                Author = GetAuthorByUsername(postUsernameElement.Value),
                Body = postBodyElement.Value,
                Excerpt = postExcerptElement?.Value,
                PublishDate = DateTime.Parse(postPublishDateElement.Value),
                Slug = postSlugElement.Value,
                Title = postTitleElement.Value
            };

            // get categories and tags
            var wpCategoriesElements = postElement.Elements("category");
            foreach (var wpCategory in wpCategoriesElements)
            {
                var domainAttribute = wpCategory.Attribute("domain");
                if (domainAttribute == null)
                    throw new XmlException("Unable to parse malformed WordPress categorization.");

                if (domainAttribute.Value == "category")
                    post.Categories.Add(new Category
                    {
                        Slug = wpCategory.Attribute("nicename")?.Value, Name = wpCategory.Value
                    });
                else if (domainAttribute.Value == "post_tag")
                    post.Tags.Add(new Tag {Slug = wpCategory.Attribute("nicename")?.Value, Name = wpCategory.Value});
            }

            // get featured image
            var postMetaElements = postElement.Elements(WordPressNamespace + "postmeta");
            foreach (var postMeta in postMetaElements)
            {
                var metaKeyElement = postMeta.Element(WordPressNamespace + "meta_key");
                if (metaKeyElement?.Value == "_thumbnail_id")
                {
                    var metaValueElement = postMeta.Element(WordPressNamespace + "meta_value");
                    if (metaValueElement?.Value != null)
                    {
                        var attachmentId = int.Parse(metaValueElement.Value);
                        post.FeaturedImage = GetAttachmentById(attachmentId);
                        break;
                    }
                }
            }

            return post;
        }
    }
}