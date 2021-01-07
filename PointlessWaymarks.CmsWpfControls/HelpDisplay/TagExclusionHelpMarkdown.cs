namespace PointlessWaymarks.CmsWpfControls.HelpDisplay
{
    public static class TagExclusionHelpMarkdown
    {
        public static string HelpBlock =>
            @"
### Tag Exclusions

For most content you will enter tags in an
editor and it is fairly natural to keep all the tags you enter appropriate for public display on 
your site. However in the case of Photographs - and in some cases Links - tags might have been
generated in another context (for example in Lightroom for your personal Photo Catalog) and not appropriate for 
public display.

Tag Exclusions are a way to exclude certain tags from visual display. For example you might create content 
from photos that are tagged with a friends name - great for some sites and uses, but on some sites it might
not be appropriate to create a way for any user to get to a page of photos tagged with your friends name thru
the public tag search page in a single click...

Excluded Tags will not appear in the Tag List for Content Pages or in the Tags Search Page and a Tag
page will not be generated. Excluded tags will not be shown on the the Main Content Search
Page - but they will be in the html as data tags and can both be searched for and seen in the source.
";
    }
}