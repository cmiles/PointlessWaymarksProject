using System.Xml.Linq;

namespace PointlessWaymarks.FeedReader.Feeds.Atom;

/// <summary>
/// Atom 1.0 link according to specification: https://validator.w3.org/feed/docs/atom.html#link
/// </summary>
public class AtomLink
{
    /// <summary>
    /// The "href" element
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    /// The "rel" element
    /// </summary>
    public string? Relation { get; set; } // rel

    /// <summary>
    /// The "type" element
    /// </summary>
    public string? LinkType { get; set; } // type

    /// <summary>
    /// The "hreflang" element
    /// </summary>
    public string? HrefLanguage { get; set; } // hreflang

    /// <summary>
    /// The "title" element
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The "length" element as int
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AtomLink"/> class.
    /// default constructor (for serialization)
    /// </summary>
    public AtomLink()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AtomLink"/> class.
    /// Reads an atom link based on the xml given in element
    /// </summary>
    /// <param name="element">link as xml</param>
    public AtomLink(XElement element)
    {
        Href = element.GetAttributeValue("href");
        Relation = element.GetAttributeValue("rel");
        LinkType = element.GetAttributeValue("type");
        HrefLanguage = element.GetAttributeValue("hreflang");
        Title = element.GetAttributeValue("title");
        Length = Helpers.TryParseInt(element.GetAttributeValue("length"));
    }

    /// <summary>
    /// Returns the Href of the link
    /// </summary>
    /// <returns>the href of the link</returns>
    public override string? ToString()
    {
        return Href;
    }
}