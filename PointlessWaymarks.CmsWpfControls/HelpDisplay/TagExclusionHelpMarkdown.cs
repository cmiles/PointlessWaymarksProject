namespace PointlessWaymarks.CmsWpfControls.HelpDisplay
{
    public static class TagExclusionHelpMarkdown
    {
        public static string HelpBlock =>
            @"
### Tag Search Exclusions

For most content you will enter tags into this program with the intent to display them with the content you are 
creating so it is fairly natural to keep all the tags you enter appropriate for public display.

However in the case of Photographs - and in some cases Links and other content - tags might have been generated 
in another program/context (for example in Lightroom for your personal photo catalog) where you 
might fill in information that is not as appropriate for public display.

Tag Search Exclusions are a way to make tags more discrete. Excluded Tags won't appear in various search pages generated 
by this program, the tag page will not be automatically linked to anything and the header of the Tag Page will
indicate to search engines not to index the tag page.

However, if you have a tag you want to completely 'exclude' and keep completely private DO NOT ENTER THE TAG
INTO THIS SOFTWARE!!!!! While these exclusions allow a tag to be more discrete information about the tag will
absolutely still be generated and present/visible on the public version of the site - and while the header of the Tag Page
may 'ask' search engines not to index that page search engines DO NOT always respect that request...
";
    }
}