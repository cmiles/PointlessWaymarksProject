namespace PointlessWaymarks.CmsWpfControls.HelpDisplay;

public static class GeoJsonContentHelpMarkdown
{
    public static string HelpBlock =>
        @"
### GeoJson Content

GeoJson content can be loaded from a file or from the clipboard. This content editor doesn't provide any way to edit GeoJson but you can find a number of good online editing solutions and for a full featured open source geographic information system see [QGIS](https://www.qgis.org/en/site/).

Requirements:
 - The GeoJson you are importing must have at least on Feature inside a FeatureCollection - the import can not handle 'raw' shapes/features.
 - The CRS for all GeoJSON objects must be WGS-84, equivalent to urn:ogc:def:crs:OGC::CRS84 (in many GeoJson specific editors this will be the default - working in QGIS, esp. with imported layers, you may need to specify this when you export data).

There is support for a limited set of formatting options, all of these should be properties on the feature:
 - title - This will appear at the top of the GeoJson feature's PopUp (shown when clicked) 
 - title-link - if a title and title-link are present the title will be presented as a link with the href (link destination) set to the title-link as long as the link isn't the current page. If a title is not included the title-link will not be used. In addition to using the title-link to link to external sites or other pages the use of the setting the special value {{self}} as the title-link value gives an easy way to link back to the GeoJson page when the GeoJson map is used in another page.
 - description - will be shown in the feature's PopUp (under the title if a title is present).
 - stroke - the color of the lines
 - stroke-width - the width of the lines (number greater than 0)
 - fill - the interior color of a polygon
 - fill-opacity - the opacity of the interior of a polygon - from 0 (transparent) to 1 (fully opaque)
";
}