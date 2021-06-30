namespace PointlessWaymarks.CmsWpfControls.HelpDisplay
{
    public static class MenuLinksHelpMarkdown
    {
        public static string HelpBlock =>
            @"
### Menu Links

Menu Link entries will be processed into a simple navigation menu that appears at the top and bottom of most pages. While
the editor will allow you to place 'anything' in these content slots the intent is for these to be links - an 
html <a/> tag, PointlessWaymarks bracket code or Markdown style link - with a short/minimal text description.

A simple but useful menu could consist of the following entries:
    {{index; text Home;}}
    {{searchpage; text Search;}}
    {{photogallerypage; text Photos;}}
    {{tagspage; text Tags;}}
    {{linklistpage; text Links;}}
";
    }
}