using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PointlessWaymarks.FeedReader.Tests
{
    [TestClass]
    public class ReaderTest
    {
        [TestMethod]
        public async Task TestDownload400BadRequest()
        {
            // results in a 400 BadRequest if webclient is not initialized correctly
            await DownloadTestAsync("http://www.methode.at/blog?format=RSS").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestAcceptHeaderForbiddenWithParsing()
        {
            // results in 403 Forbidden if webclient does not have the accept header set
            var feed = await Reader.ReadAsync("http://www.girlsguidetopm.com/feed/").ConfigureAwait(false);
            var title = feed.Title;
            Assert.IsTrue(feed.Items.Count > 2);
            Assert.IsTrue(!string.IsNullOrEmpty(title));
        }

        [TestMethod]
        public async Task TestAcceptForbiddenUserAgent()
        {
            // results in 403 Forbidden if webclient does not have the accept header set
            await DownloadTestAsync("https://mikeclayton.wordpress.com/feed/").ConfigureAwait(false);
        }


        [TestMethod]
        public async Task TestAcceptForbiddenUserAgentWrike()
        {
            // results in 403 Forbidden if webclient does not have the accept header set
            await DownloadTestAsync("https://www.wrike.com/blog").ConfigureAwait(false);
        }


        [TestMethod]
        public async Task TestParseRssLinksCodehollow()
        {
            await TestParseRssLinksAsync("https://codehollow.com", 2).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestParseRssLinksNYTimes() { await TestParseRssLinksAsync("nytimes.com", 1).ConfigureAwait(false); }

        private static async Task TestParseRssLinksAsync(string url, int expectedNumberOfLinks)
        {
            var urls = await Reader.ParseFeedUrlsAsStringAsync(url).ConfigureAwait(false);
            Assert.AreEqual(expectedNumberOfLinks, urls.Length);
        }

        [TestMethod]
        public async Task TestParseAndAbsoluteUrlDerStandard1()
        {
            var url = "derstandard.at";
            var links = await Reader.GetFeedUrlsFromUrlAsync(url).ConfigureAwait(false);

            foreach (var link in links)
            {
                var absoluteUrl = Reader.GetAbsoluteFeedUrl(url, link);
                Assert.IsTrue(absoluteUrl.Url.StartsWith("http://"));
            }

        }

        [TestMethod]
        public async Task TestReadSimpleFeed()
        {
            var feed = await Reader.ReadAsync("https://arminreiter.com/feed").ConfigureAwait(false);
            var title = feed.Title;
            Assert.AreEqual("arminreiter.com", title);
            Assert.AreEqual(10, feed.Items.Count());
        }

        [TestMethod]
        public async Task TestReadRss20GermanFeed()
        {
            var feed = await Reader.ReadAsync("http://guidnew.com/feed").ConfigureAwait(false);
            var title = feed.Title;
            Assert.AreEqual("Guid.New", title);
            Assert.IsTrue(feed.Items.Count > 0);
        }

        [TestMethod]
        public async Task TestReadRss10GermanFeed()
        {
            var feed = await Reader.ReadAsync("http://rss.orf.at/news.xml").ConfigureAwait(false);
            var title = feed.Title;
            Assert.AreEqual("news.ORF.at", title);
            Assert.IsTrue(feed.Items.Count > 10);
        }

        [TestMethod]
        public async Task TestReadAtomFeedHeise()
        {
            var feed = await Reader.ReadAsync("https://www.heise.de/newsticker/heise-atom.xml").ConfigureAwait(false);
            Assert.IsTrue(!string.IsNullOrEmpty(feed.Title));
            Assert.IsTrue(feed.Items.Count > 1);
        }

        [TestMethod]
        public async Task TestReadAtomFeedGitHub()
        {
            try
            {
                var feed = await Reader.ReadAsync("http://github.com/codehollow/AzureBillingRateCardSample/commits/master.atom").ConfigureAwait(false);
                //Assert.IsTrue(!string.IsNullOrEmpty(feed.Title));
            }
            catch (Exception ex)
            {
                Assert.AreEqual(ex.InnerException.GetType(), typeof(System.Net.WebException));
                Assert.AreEqual(ex.InnerException.Message, "The request was aborted: Could not create SSL/TLS secure channel.");
            }
            
        }

        [TestMethod]
        public async Task TestReadRss20GermanFeedPowershell()
        {
            var feed = await Reader.ReadAsync("http://www.powershell.co.at/feed/").ConfigureAwait(false);
            Assert.IsTrue(!string.IsNullOrEmpty(feed.Title));
            Assert.IsTrue(feed.Items.Count > 0);
        }

        [TestMethod]
        public async Task TestReadRss20FeedCharter97Handle403Forbidden()
        {
            var feed = await Reader.ReadAsync("charter97.org/rss.php").ConfigureAwait(false);
            Assert.IsTrue(!string.IsNullOrEmpty(feed.Title));
        }

        [TestMethod]
        public async Task TestReadRssScottHanselmanWeb()
        {
            var feed = await Reader.ReadAsync("http://feeds.hanselman.com/ScottHanselman").ConfigureAwait(false);
            Assert.IsTrue(!string.IsNullOrEmpty(feed.Title));
            Assert.IsTrue(feed.Items.Count > 0);
        }

        [TestMethod]
        public async Task TestReadBuildAzure()
        {
            await DownloadTestAsync("https://buildazure.com").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestReadNoticiasCatolicas()
        {
            var feed = await Reader.ReadAsync("feeds.feedburner.com/NoticiasCatolicasAleteia").ConfigureAwait(false);
            Assert.AreEqual("Noticias Catolicas", feed.Title);
            Assert.IsTrue(feed.Items.Count > 0);
        }

        [TestMethod]
        public async Task TestReadTimeDoctor()
        {
            var feed = await Reader.ReadAsync("https://www.timedoctor.com/blog/feed/").ConfigureAwait(false);
            Assert.AreEqual("Time Doctor Blog", feed.Title);
            Assert.IsTrue(feed.Items.Count > 0);
        }

        [TestMethod]
        public async Task TestReadMikeC()
        {
            var feed = await Reader.ReadAsync("https://mikeclayton.wordpress.com/feed/").ConfigureAwait(false);
            Assert.AreEqual("Shift Happens!", feed.Title);
            Assert.IsTrue(feed.Items.Count > 0);
        }

        [TestMethod]
        public async Task TestReadTheLPM()
        {
            var feed = await Reader.ReadAsync("https://thelazyprojectmanager.wordpress.com/feed/").ConfigureAwait(false);
            Assert.AreEqual("The Lazy Project Manager's Blog", feed.Title);
            Assert.IsTrue(feed.Items.Count > 0);
        }

        [TestMethod]
        public async Task TestReadTechRep()
        {
            var feed = await Reader.ReadAsync("http://www.techrepublic.com/rssfeeds/topic/project-management/").ConfigureAwait(false);
            Assert.AreEqual("Project Management Articles & Tutorials | TechRepublic", feed.Title);
            Assert.IsTrue(feed.Items.Count > 0);
        }

        [TestMethod]
        public async Task TestReadAPOD()
        {
            var feed = await Reader.ReadAsync("https://apod.nasa.gov/apod.rss").ConfigureAwait(false);
            Assert.AreEqual("APOD", feed.Title);
            Assert.IsTrue(feed.Items.Count > 0);
        }

        [TestMethod]
        public async Task TestReadThaqafnafsak()
        {
            var feed = await Reader.ReadAsync("http://www.thaqafnafsak.com/feed").ConfigureAwait(false);
            Assert.AreEqual("ثقف نفسك", feed.Title);
            Assert.IsTrue(feed.Items.Count > 0);
        }

        [TestMethod]
        public async Task TestReadLiveBold()
        {
            var feed = await Reader.ReadAsync("http://feeds.feedburner.com/LiveBoldAndBloom").ConfigureAwait(false);
            Assert.AreEqual("Live Bold and Bloom", feed.Title);
            Assert.IsTrue(feed.Items.Count > 0);
        }

        [TestMethod]
        public async Task TestSwedish_ISO8859_1()
        {
            var feed = await Reader.ReadAsync("https://www.retriever-info.com/feed/2004645/intranet30/index.xml");
            Assert.AreEqual("intranet30", feed.Title);
        }

        [TestMethod]
        public async Task TestStadtfeuerwehrWeiz_ISO8859_1()
        {
            var feed = await Reader.ReadAsync("http://www.stadtfeuerwehr-weiz.at/rss/einsaetze.xml");
            Assert.AreEqual("Stadtfeuerwehr Weiz - Einsätze", feed.Title);
        }

        private static async Task DownloadTestAsync(string url)
        {
            var content = await Helpers.DownloadAsync(url).ConfigureAwait(false);
            Assert.IsTrue(content.Length > 200);
        }
    }
}
