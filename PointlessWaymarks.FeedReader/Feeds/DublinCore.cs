using System.Xml.Linq;

namespace PointlessWaymarks.FeedReader.Feeds;

/// <summary>
/// The parsed "dc:" elements in a feed
/// </summary>
public class DublinCore
{
    /// <summary>
    /// The "title" element
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The "creator" element
    /// </summary>
    public string? Creator { get; set; }

    /// <summary>
    /// The "subject" element
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// The "description" field
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The "publisher" element
    /// </summary>
    public string? Publisher { get; set; }

    /// <summary>
    /// The "contributor" element
    /// </summary>
    public string? Contributor { get; set; }

    /// <summary>
    /// The "date" element
    /// </summary>
    public string? DateString { get; set; }

    /// <summary>
    /// The "date" element as datetime. Null if parsing failed or date is empty.
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// The "type" element
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// The "format" element
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// The "identifier" element
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// The "source" element
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// The "language" element
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// The "rel" element
    /// </summary>
    public string? Relation { get; set; }

    /// <summary>
    /// The "coverage" element
    /// </summary>
    public string? Coverage { get; set; }

    /// <summary>
    /// The "rights" element
    /// </summary>
    public string? Rights { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DublinCore"/> class.
    /// default constructor (for serialization)
    /// </summary>
    public DublinCore()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DublinCore"/> class.
    /// Reads a dublincore (dc:) element based on the xml given in element
    /// </summary>
    /// <param name="item">item element as xml</param>
    public DublinCore(XElement? item)
    {
        Title = item.GetValue("dc:title");
        Creator = item.GetValue("dc:creator");
        Subject = item.GetValue("dc:subject");
        Description = item.GetValue("dc:description");
        Publisher = item.GetValue("dc:publisher");
        Contributor = item.GetValue("dc:contributor");
        DateString = item.GetValue("dc:date");
        Date = Helpers.TryParseDateTime(DateString);
        Type = item.GetValue("dc:type");
        Format = item.GetValue("dc:format");
        Identifier = item.GetValue("dc:identifier");
        Source = item.GetValue("dc:source");
        Language = item.GetValue("dc:language");
        Relation = item.GetValue("dc:relation");
        Coverage = item.GetValue("dc:coverage");
        Rights = item.GetValue("dc:rights");
    }
}