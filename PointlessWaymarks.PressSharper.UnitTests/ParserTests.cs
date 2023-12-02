using NUnit.Framework;

namespace PointlessWaymarks.PressSharper.UnitTests;

[TestFixture]
public class BlogTests
{
    private const string WordPressXml = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
                <rss version=""2.0""
	                xmlns:excerpt=""http://wordpress.org/export/1.2/excerpt/""
	                xmlns:content=""http://purl.org/rss/1.0/modules/content/""
	                xmlns:wfw=""http://wellformedweb.org/CommentAPI/""
	                xmlns:dc=""http://purl.org/dc/elements/1.1/""
	                xmlns:wp=""http://wordpress.org/export/1.2/"">
                    <channel>
                        <title>foo title</title>
                        <description>foo description</description>
                        <wp:author>
                            <wp:author_id>1</wp:author_id>
                            <wp:author_login>johndoe</wp:author_login>
                            <wp:author_email>johndoe@gmail.com</wp:author_email>
                            <wp:author_display_name><![CDATA[John Doe]]></wp:author_display_name>
                        </wp:author>
                        <wp:author>
                            <wp:author_id>2</wp:author_id>
                            <wp:author_login>bobsmith</wp:author_login>
                            <wp:author_email>bobsmith@gmail.com</wp:author_email>
                            <wp:author_display_name><![CDATA[Bob Smith]]></wp:author_display_name>
                        </wp:author>
                        <item>
		                    <title>test title 1</title>
		                    <dc:creator>johndoe</dc:creator>
		                    <content:encoded><![CDATA[test body 1]]></content:encoded>
		                    <wp:post_date>2010-03-05 06:12:10</wp:post_date>
		                    <wp:post_name>test-title-1</wp:post_name>
		                    <wp:status>publish</wp:status>
		                    <wp:post_type>post</wp:post_type>
		                    <category domain=""category"" nicename=""category-one""><![CDATA[Category One]]></category>
                            <category domain=""category"" nicename=""category-two""><![CDATA[Category Two]]></category>
		                    <category domain=""post_tag"" nicename=""tag-one""><![CDATA[Tag One]]></category>
                            <wp:postmeta>
			                    <wp:meta_key><![CDATA[_thumbnail_id]]></wp:meta_key>
			                    <wp:meta_value><![CDATA[3]]></wp:meta_value>
		                    </wp:postmeta>
	                    </item>
                        <item>
		                    <title>test title 2</title>
		                    <dc:creator>bobsmith</dc:creator>
		                    <content:encoded><![CDATA[test body 2]]></content:encoded>
		                    <wp:post_date>2011-04-08 09:58:10</wp:post_date>
		                    <wp:post_name>test-title-2</wp:post_name>
		                    <wp:status>publish</wp:status>
		                    <wp:post_type>post</wp:post_type>
		                    <category domain=""category"" nicename=""category-three""><![CDATA[Category Three]]></category>
		                    <category domain=""post_tag"" nicename=""tag-two""><![CDATA[Tag Two]]></category>
		                    <category domain=""post_tag"" nicename=""tag-three""><![CDATA[Tag Three]]></category>
	                    </item>
                        <item>
		                    <title>About</title>
		                    <dc:creator>johndoe</dc:creator>
		                    <content:encoded><![CDATA[This is the about page]]></content:encoded>
		                    <wp:post_id>1</wp:post_id>
		                    <wp:post_parent>0</wp:post_parent>
		                    <wp:post_date>2012-05-09 09:58:10</wp:post_date>
		                    <wp:post_name>about</wp:post_name>
		                    <wp:status>publish</wp:status>
		                    <wp:post_type>page</wp:post_type>
	                    </item>
                        <item>
		                    <title>Contact Us</title>
		                    <dc:creator>bobsmith</dc:creator>
		                    <content:encoded><![CDATA[This is the contact page]]></content:encoded>
		                    <wp:post_id>2</wp:post_id>
		                    <wp:post_parent>1</wp:post_parent>
		                    <wp:post_date>2013-06-13 09:58:10</wp:post_date>
		                    <wp:post_name>contact-us</wp:post_name>
		                    <wp:status>publish</wp:status>
		                    <wp:post_type>page</wp:post_type>
	                    </item>
                        <item>
		                    <title>Featured Image</title>
		                    <wp:post_id>3</wp:post_id>
                            <wp:attachment_url><![CDATA[http://www.example.com/featured.jpg]]></wp:attachment_url>
                            <wp:post_type>attachment</wp:post_type>
	                    </item>
                    </channel>
                </rss>";

    [Test]
    public void Can_Parse_Blog_Title()
    {
        var blog = new Blog(WordPressXml);

        Assert.That(blog.Title, Is.EqualTo("foo title"));
    }

    [Test]
    public void Can_Parse_Blog_Description()
    {
        var blog = new Blog(WordPressXml);

        Assert.That(blog.Description, Is.EqualTo("foo description"));
    }

    [Test]
    public void Can_Parse_Authors()
    {
        var blog = new Blog(WordPressXml);
        var authors = blog.Authors.ToList();

        Assert.That(authors.Count, Is.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(authors[0].Id, Is.EqualTo(1));
            Assert.That(authors[0].Username, Is.EqualTo("johndoe"));
            Assert.That(authors[0].Email, Is.EqualTo("johndoe@gmail.com"));

            Assert.That(authors[1].Id, Is.EqualTo(2));
            Assert.That(authors[1].Username, Is.EqualTo("bobsmith"));
            Assert.That(authors[1].Email, Is.EqualTo("bobsmith@gmail.com"));
        });
    }

    [Test]
    public void Can_Parse_Attachments()
    {
        var blog = new Blog(WordPressXml);
        var attachments = blog.Attachments.ToList();

        Assert.That(attachments.Count, Is.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(attachments[0].Id, Is.EqualTo(3));
            Assert.That(attachments[0].Title, Is.EqualTo("Featured Image"));
            Assert.That(attachments[0].Url, Is.EqualTo("http://www.example.com/featured.jpg"));
        });
    }

    [Test]
    public void Can_Parse_Posts()
    {
        var blog = new Blog(WordPressXml);
        var posts = blog.GetPosts().ToList();

        Assert.That(posts.Count, Is.EqualTo(2));

        Assert.Multiple(() =>
        {
            // post 1
            Assert.That(posts[0].Title, Is.EqualTo("test title 1"));
            Assert.That(posts[0].Author?.Username, Is.EqualTo("johndoe"));
            Assert.That(posts[0].Body, Is.EqualTo("test body 1"));
            Assert.That(posts[0].PublishDate?.ToShortDateString(), Is.EqualTo("3/5/2010"));
            Assert.That(posts[0].Slug, Is.EqualTo("test-title-1"));
            Assert.That(posts[0].Categories.Count, Is.EqualTo(2));
        });
        Assert.Multiple(() =>
        {
            Assert.That(posts[0].Categories[0].Slug, Is.EqualTo("category-one"));
            Assert.That(posts[0].Categories[0].Name, Is.EqualTo("Category One"));
            Assert.That(posts[0].Categories[1].Slug, Is.EqualTo("category-two"));
            Assert.That(posts[0].Categories[1].Name, Is.EqualTo("Category Two"));
            Assert.That(posts[0].Tags.Count, Is.EqualTo(1));
        });
        Assert.Multiple(() =>
        {
            Assert.That(posts[0].Tags[0].Slug, Is.EqualTo("tag-one"));
            Assert.That(posts[0].Tags[0].Name, Is.EqualTo("Tag One"));

            // post 1 featured image
            Assert.That(posts[0].FeaturedImage, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(posts[0].FeaturedImage?.Id, Is.EqualTo(3));
            Assert.That(posts[0].FeaturedImage?.Title, Is.EqualTo("Featured Image"));
            Assert.That(posts[0].FeaturedImage?.Url, Is.EqualTo("http://www.example.com/featured.jpg"));

            // post 2
            Assert.That(posts[1].Title, Is.EqualTo("test title 2"));
            Assert.That(posts[1].Author?.Username, Is.EqualTo("bobsmith"));
            Assert.That(posts[1].Body, Is.EqualTo("test body 2"));
            Assert.That(posts[1].PublishDate?.ToShortDateString(), Is.EqualTo("4/8/2011"));
            Assert.That(posts[1].Slug, Is.EqualTo("test-title-2"));
            Assert.That(posts[1].Categories.Count, Is.EqualTo(1));
        });
        Assert.Multiple(() =>
        {
            Assert.That(posts[1].Categories[0].Slug, Is.EqualTo("category-three"));
            Assert.That(posts[1].Categories[0].Name, Is.EqualTo("Category Three"));
            Assert.That(posts[1].Tags.Count, Is.EqualTo(2));
        });
        Assert.Multiple(() =>
        {
            Assert.That(posts[1].Tags[0].Slug, Is.EqualTo("tag-two"));
            Assert.That(posts[1].Tags[0].Name, Is.EqualTo("Tag Two"));
            Assert.That(posts[1].Tags[1].Slug, Is.EqualTo("tag-three"));
            Assert.That(posts[1].Tags[1].Name, Is.EqualTo("Tag Three"));

            // post 2 featured image
            Assert.That(posts[1].FeaturedImage, Is.Null);
        });
    }

    [Test]
    public void Can_Parse_Pages()
    {
        var blog = new Blog(WordPressXml);
        var pages = blog.GetPages().ToList();

        Assert.That(pages.Count, Is.EqualTo(2));

        Assert.Multiple(() =>
        {
            // page 1
            Assert.That(pages[0].Id, Is.EqualTo(1));
            Assert.That(pages[0].ParentId, Is.Null);
            Assert.That(pages[0].Title, Is.EqualTo("About"));
            Assert.That(pages[0].Author?.Username, Is.EqualTo("johndoe"));
            Assert.That(pages[0].Body, Is.EqualTo("This is the about page"));
            Assert.That(pages[0].PublishDate.ToShortDateString(), Is.EqualTo("5/9/2012"));
            Assert.That(pages[0].Slug, Is.EqualTo("about"));

            // page 2
            Assert.That(pages[1].Id, Is.EqualTo(2));
            Assert.That(pages[1].ParentId, Is.EqualTo(1));
            Assert.That(pages[1].Title, Is.EqualTo("Contact Us"));
            Assert.That(pages[1].Author?.Username, Is.EqualTo("bobsmith"));
            Assert.That(pages[1].Body, Is.EqualTo("This is the contact page"));
            Assert.That(pages[1].PublishDate.ToShortDateString(), Is.EqualTo("6/13/2013"));
            Assert.That(pages[1].Slug, Is.EqualTo("contact-us"));
        });
    }
}