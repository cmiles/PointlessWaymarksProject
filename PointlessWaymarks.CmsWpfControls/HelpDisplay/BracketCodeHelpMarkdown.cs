namespace PointlessWaymarks.CmsWpfControls.HelpDisplay;

public static class BracketCodeHelpMarkdown
{
    public static string HelpBlock =>
        @"
### Content Codes

#### User Content Codes

To create links to site content use the 'Bracket Codes' below - using these, rather than html links, will allow the URL and Titles of linked content to update automatically. Because the content referenced by a Code is identified by a rather long ContentId the intent is for you to use the PointlessWaymarksCMS software to generate these codes rather than write them by hand.

All of the codes below have an area for an identifier text or note - this text does NOT impact the link or displayed text in any way, it is just for easy reference. While you can use the code variations below that let you specify the display text the nice detail about not specifying text is that by default the title of the content will be used which means that the display text will change if the Title of the content changes (once the html is regenerated...)

Codes can not contain {{ or }}.

Basic code formats - the [*description*] blocks should be replaced:
 - {{[*link type (see below)*] [*ContentId*]; [*title or note - for reference only*]}} - displays a link to the content with the current Title of the content as the display text.
 - {{[*link type (see below)*] [*ContentId*]; text [*your display text*]; [*title or note - for reference only*]}} - displays a link to the content with the specified display text (don't forget to include 'text ' for this to work).

Link Types:
 - dailyphotoslink - links to a Daily Photo Page
 - filelink - text link to the File Page
 - fileimagelink - image link to the File Page
 - fileembed - if possible/supported 'embeds' the file into a player/viewer - pdf, common video and common sound files are supported
 - filedownloadlink - text link to the File itself - this is a 'download' link and will only work correctly if direct downloads are enabled for the file
 - fileurl - the url for the file referenced by the file content - this does not create a link or do anything other than return the url for the referenced file as text - this can be used to use the file in something like an html audio tag
 - geojson - map with GeoJson
 - geojsonimagelink - image link to the GeoJson Page
 - geojsonlink - text link to the GeoJson Page
 - image - image ink to the Image Page
 - imagelink - text link to the Image Page
 - line - map with Line
 - lineelevationchart - Line Elevation Chart
 - lineimagelink - image link to the Line Page
 - linelink - text link to the Line Page
 - linestats - Line Stats
 - mapcomponent - Map
 - notelink - text link to the Note Page
 - photo - image link to the Photo Page
 - photolink - text link to the Photo Page
 - point - map with Points
 - pointexternaldirectionslink - link to external directions for a Point
 - pointimagelink - image link to the Point Page
 - pointlink - text link to the Point Page
 - postlink - text link to the Post Page
 - postimage - image link to a Post Page
 - videoimagelink - image link to the Video Page
 - videolink - text link to the Video Page
 - videoembed - if possible/supported 'embeds' the video into a player/viewer

Picture Gallery: Content types with a Main Picture can be used inside a picture gallery to create a compact view of a number of images. Content without a Main Picture will be skipped and the gallery will not honor your content order.
 - [[picture gallery {{Include as Many Bracket Codes as you Want}}{{Include as Many Bracket Codes as you Want}}]]


#### Site Codes

This set of bracket codes is designed to make it simpler to link to the main pages and RSS feeds for the site.

Basic code formats - the [*description*] blocks should be replaced:
 - {{[*link type (see below)*];}} - displays a link with the default text listed below (the trailing semicolon is required)
 - {{[*link type (see below)*]; text [*your display text*];}} - displays a link with the text specified in [*your display test*] (the trailing semicolon is required)

Link Types:
 - index, Main
 - photogallerypage, Photos
 - searchpage, Search
 - tagspage, Tags
 - linklistpage, Links
 - indexrss, Main Page RSS Feed
 - filerss, Files RSS Feed
 - imagerss, Images RSS Feed
 - linkrss, Links RSS Feed
 - noterss, Notes RSS Feed
 - photorss, Photo Gallery RSS Feed
";
}