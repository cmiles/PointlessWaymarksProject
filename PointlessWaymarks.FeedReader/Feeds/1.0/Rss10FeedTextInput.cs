using System.Xml.Linq;
using PointlessWaymarks.FeedReader.Feeds.Base;

namespace PointlessWaymarks.FeedReader.Feeds._1._0;

/// <summary>
/// Rss 1.0 Feed textinput according to specification: http://web.resource.org/rss/1.0/spec
/// </summary>
public class Rss10FeedTextInput : FeedTextInput
{
    /// <summary>
    /// The "about" attribute of the element
    /// </summary>
    public string? About { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rss10FeedTextInput"/> class.
    /// default constructor (for serialization)
    /// </summary>
    public Rss10FeedTextInput()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rss10FeedTextInput"/> class.
    /// Reads a rss 1.0 textInput element based on the xml given in item
    /// </summary>
    /// <param name="element">about element as xml</param>
    public Rss10FeedTextInput(XElement? element)
        : base(element)
    {
        About = element.GetAttribute("rdf:about").GetValue();
    }
}