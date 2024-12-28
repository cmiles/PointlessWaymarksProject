using System.Text;
using System.Xml;
using System.Xml.Linq;
using PointlessWaymarks.FeedReader.Parser.Exceptions;

namespace PointlessWaymarks.FeedReader.Parser;

/// <summary>
///     Internal FeedParser - returns the type of the feed or the parsed feed.
/// </summary>
internal static class FeedParser
{
    /// <summary>
    ///     Extracts the undeclared entity from and XML exception message - the intent is that
    ///     this can then be removed from the XML content and the parsing retried.
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    private static (string entity, int lineNumber, int linePosition) ExtractUndeclaredEntityInformation(
        XmlException exception)
    {
        var startIndex = exception.Message.IndexOf("undeclared entity '", StringComparison.Ordinal) +
                         "undeclared entity '".Length;
        var endIndex = exception.Message.IndexOf("'", startIndex, StringComparison.Ordinal);
        if (startIndex > 0 && endIndex > startIndex)
            return ($"&{exception.Message[startIndex..endIndex]};", exception.LineNumber, exception.LinePosition - 1);
        return (string.Empty, exception.LineNumber, exception.LinePosition - 1);
    }

    /// <summary>
    ///     reads the encoding from a feed document, returns UTF8 by default
    /// </summary>
    /// <param name="feedDoc">rss feed document</param>
    /// <returns>encoding or utf8 by default</returns>
    private static Encoding GetEncoding(XDocument feedDoc)
    {
        var encoding = Encoding.UTF8;

        try
        {
            var encodingStr = feedDoc.Document?.Declaration?.Encoding;
            if (!string.IsNullOrEmpty(encodingStr))
                encoding = Encoding.GetEncoding(encodingStr);
        }
        catch (Exception)
        {
            // ignore and return default encoding
        }

        return encoding;
    }

    /// <summary>
    ///     Returns the parsed feed.
    ///     This method checks the encoding of the received file
    /// </summary>
    /// <param name="feedContentData">the feed document</param>
    /// <returns>parsed feed</returns>
    public static Feed GetFeed(byte[] feedContentData)
    {
        //2024-06-24: Found that https://brucepercy.co.uk/blog?format=rss should be decoded as Unicode, so
        //added a 'happy path' Unicode decode before returning to UTF8 decode and the original logic. This
        //will fail if the feed is Unicode, but it doesn't start with the xml declaration...
        var feedContent = Encoding.Unicode.GetString(feedContentData);
        if (!feedContent.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            feedContent = Encoding.UTF8.GetString(feedContentData);

        feedContent = RemoveWrongChars(feedContent);

        XDocument? feedDoc = null; // 2.) read document to get the used encoding
        var tryProcess = true;
        var tryProcessCount = 0;

        //2024-12-27: 50 is arbitrary...
        while (tryProcess && tryProcessCount < 50)
        {
            tryProcessCount++;
            tryProcess = false;

            try
            {
                feedDoc = XDocument.Parse(feedContent);
            }
            catch (XmlException ex) when (ex.Message.Contains("Reference to undeclared entity"))
            {
                // Extract the undeclared entity from the exception message
                var entityExceptionInformation = ExtractUndeclaredEntityInformation(ex);
                if (!string.IsNullOrEmpty(entityExceptionInformation.entity))
                {
                    var undeclaredEntities = new Dictionary<string, string>
                    {
                        { "&rsquo;", "&#8217;" },
                        { "&lsquo;", "&#8216;" },
                        { "&rdquo;", "&#8221;" },
                        { "&ldquo;", "&#8220;" },
                        { "&nbsp;", "&#160;" }
                    };

                    // Remove the specific undeclared entity and retry parsing
                    var replacement = undeclaredEntities.GetValueOrDefault(entityExceptionInformation.entity, " ");
                    feedContent = ReplaceStringAtLineAndPositionOrAllContentsIfNotFound(feedContent,
                        entityExceptionInformation.entity, entityExceptionInformation.lineNumber,
                        entityExceptionInformation.linePosition, replacement);
                    tryProcess = true;
                }
                else
                {
                    throw;
                }
            }
        }

        if (feedDoc == null) throw new Exception("FeedDoc is null after parsing");

        var encoding = GetEncoding(feedDoc); // 3.) get used encoding

        if (!Equals(encoding, Encoding.UTF8)) // 4.) if not UTF8 - reread the data :
            // in some cases - ISO-8859-1 - Encoding.UTF8.GetString doesn't work correct, so converting
            // from UTF8 to ISO-8859-1 doesn't work and result is wrong. see: FullParseTest.TestRss20ParseSwedishFeedWithIso8859_1
        {
            feedContent = encoding.GetString(feedContentData);
            feedContent = RemoveWrongChars(feedContent);
        }

        var feedType = ParseFeedType(feedDoc);

        var parser = Factory.GetParser(feedType);
        var feed = parser.Parse(feedContent);

        return feed.ToFeed();
    }

    /// <summary>
    ///     Returns the parsed feed
    /// </summary>
    /// <param name="feedContent">the feed document</param>
    /// <returns>parsed feed</returns>
    public static Feed GetFeed(string feedContent)
    {
        // Convert the string to a byte array using UTF8 encoding
        var feedContentData = Encoding.UTF8.GetBytes(feedContent);

        // Call the GetFeed method that accepts a byte array
        return GetFeed(feedContentData);
    }

    /// <summary>
    ///     Returns the feed type - rss 1.0, rss 2.0, atom, ...
    /// </summary>
    /// <param name="doc">the xml document</param>
    /// <returns>the feed type</returns>
    public static FeedType ParseFeedType(XDocument doc)
    {
        var rootElement = doc.Root?.Name.LocalName;

        if (rootElement.EqualsIgnoreCase("feed"))
            return FeedType.Atom;

        if (rootElement.EqualsIgnoreCase("rdf"))
            return FeedType.Rss_1_0;

        if (rootElement.EqualsIgnoreCase("rss"))
        {
            var version = doc.Root?.Attribute("version")?.Value;
            if (version.EqualsIgnoreCase("2.0"))
            {
                if (doc.Root?.Attribute(XName.Get("media", XNamespace.Xmlns.NamespaceName)) != null)
                    return FeedType.MediaRss;
                else
                    return FeedType.Rss_2_0;
            }

            if (version.EqualsIgnoreCase("0.91"))
                return FeedType.Rss_0_91;

            if (version.EqualsIgnoreCase("0.92"))
                return FeedType.Rss_0_92;

            return FeedType.Rss;
        }

        throw new FeedTypeNotSupportedException($"unknown feed type {rootElement}");
    }

    /// <summary>
    ///     Removes some characters and commonly undeclared entities that can cause issues.
    /// </summary>
    /// <param name="feedContent">rss feed content</param>
    /// <returns>cleaned up rss feed content</returns>
    private static string RemoveWrongChars(string feedContent)
    {
        // replaces all control characters except CR LF (\r\n) and TAB.
        for (var charCode = 0; charCode <= 31; charCode++)
        {
            if (charCode is 0x0D or 0x0A or 0x09) continue;

            feedContent = feedContent.Replace(((char)charCode).ToString(), string.Empty);
        }

        feedContent = feedContent.Replace(((char)127).ToString(), string.Empty); // replace DEL
        feedContent =
            feedContent.Replace(((char)65279).ToString(),
                string.Empty); // replaces special char, fixes issues with at least one feed

        return feedContent.Trim();
    }

    /// <summary>
    ///     Replaces a string in the content at the given line and position - but if the value to replace is not found at
    ///     the line and position, it replaces it everywhere in the content. This is very specific to parsing the feed
    ///     content and the fallback is just-in-case there is some unexpected difference between the line and position
    ///     from the exception and the parsing done in this method.
    /// </summary>
    /// <param name="content"></param>
    /// <param name="toReplace"></param>
    /// <param name="lineNumber"></param>
    /// <param name="linePosition"></param>
    /// <param name="replacement"></param>
    /// <returns></returns>
    private static string ReplaceStringAtLineAndPositionOrAllContentsIfNotFound(string content, string toReplace,
        int lineNumber, int linePosition, string replacement)
    {
        var lines = content.Split('\n');
        if (lineNumber - 1 < lines.Length)
        {
            var line = lines[lineNumber - 1];
            if (linePosition - 1 < line.Length && line.Substring(linePosition - 1, toReplace.Length) == toReplace)
            {
                var start = line.Substring(0, linePosition - 1);
                var end = line.Substring(linePosition - 1 + toReplace.Length);
                lines[lineNumber - 1] = start + replacement + end;
                return string.Join('\n', lines);
            }
        }

        // If the value to replace is not found at the given line and position, replace it everywhere in the content
        return content.Replace(toReplace, replacement);
    }
}