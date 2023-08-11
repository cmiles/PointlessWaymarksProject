using System.Xml.Linq;

namespace PointlessWaymarks.FeedReader.Feeds;

/// <summary>
/// The parsed syndication elements. Those are all elements that start with "sy:"
/// </summary>
public class Syndication
{
    /// <summary>
    /// The "updatePeriod" element
    /// </summary>
    public string? UpdatePeriod { get; set; }

    /// <summary>
    /// The "updateFrequency" element
    /// </summary>
    public string? UpdateFrequency { get; set; }

    /// <summary>
    /// The "updateBase" element
    /// </summary>
    public string? UpdateBase { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Syndication"/> class.
    /// default constructor (for serialization)
    /// </summary>
    public Syndication()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Syndication"/> class.
    /// Creates the object based on the xml in the given XElement
    /// </summary>
    /// <param name="xElement">syndication element as xml</param>
    public Syndication(XElement? xElement)
    {
        UpdateBase = xElement.GetValue("sy:updateBase");
        UpdateFrequency = xElement.GetValue("sy:updateFrequency");
        UpdatePeriod = xElement.GetValue("sy:updatePeriod");
    }
}