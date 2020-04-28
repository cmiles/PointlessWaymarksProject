namespace PointlessWaymarksCmsWpfControls.HelpDisplay
{
    public static class BracketCodeHelpMarkdown
    {
        public static string HelpBlock =>
            @"
### Bracket Codes - Linking to Site Content

To create links to site content use the 'Bracket Codes' below - using these, rather than html links, will allow the URL and Titles of linked content to update automatically. Because the content referenced by a Bracket Code is identified by a rather long ContentId the intent is for you to use the PointlessWaymarksCMS software to generate these links rather than write them by hand.

All of the codes below have an area for an identifier text or note - this text does NOT impact the link or displayed text in any way, it is just for easy  reference. While you can use the code variations below that let you specify the display text the nice detail about not specifying text is that by default the title of the content will be used which means that the display text will change if the Title of the content changes (once the html is regenerated...)

Codes can not contain {{ or }}.

Basic code formats - the [*description*] blocks should be replaced:
 - {{[*link type (see below)*] [*ContentId*]; [*title or note - for reference only*]}} - displays a link to the content with the current Title of the content as the display text.
 - {{[*link type (see below)*] [*ContentId*]; text [*your display text*]; [*title or note - for reference only*]}} - displays a link to the content with the specified display text

Link Types:
 - filelink - a link to the File Page
 - filedownloadlink - a link to the File - this is a 'download' link and will only work correctly if direct downloads are enabled for the file
 - notelink - a link to a Note Page
 - image - displays an image with a link to the Image Page
 - postlink - a link to a Post Page
 - photo - displays a photo with a link to a Photo Page
";
    }
}