## Todos
 - Change Created and Updated style when creator and updater are the same (do via tests!) - Started, first two tests are missing
 - Link List - selection - try a click gesture (maybe disabled in ReadOnly? not sure) click to raise the for selection event
 - Sorting needs better visual indicators
 - Better startup experience when settings file and/or db are missing
 - Revisit og and dublin metadata - reference site not code and is it used correctly? Other tags that could be included?
 - Top of the page search box - maybe pass a query parameter to the all content list page?
 - Should there be a small menu at the top? Ugh.
 - Need to be able to select/switch settings files (and so associated db) - this seems much easier and happier in SQLite? That does have implications for elevation (Z) in Spatialite - but with that functionality minimal so far worth working around. Actually like the current MS Sql setup but painful compared to SQLite in terms of setup...
 - Is everything getting HMTL Encoded - are there spots with leaks?
 - RSS - Does title need CDATA to be completely safe? Or?

## Ideas
 - WebView2 (Looks like System.Interactivity plus MvvmLight are blockers here?)
 - Extract links from list page or 'all site'
 - Better header metadata on list pages
 - What if you piped/setup your google alerts to RSS and there was integrated support for pulling them in and working with them. Obvious con is not sure even if RSS is still currently an option whether it will always be an option.
 - Backup the master media directory and database
 - Restore from JSON
 - Some sort of Master JSON Backup
 - Check in on the Spatialite Z bug in EF Core and/or investigate moving to SQLite (what about an elevation lookup table vs Z values?)
 - Explore copying original media both into the content folder and keeping in the media archive folder for 'backup'

##Issues to Track
 - https://github.com/dotnet/wpf/issues/152 - Vaguely anyway tracks the issue where Xaml Islands render over all WPF content - not sure this is going anywhere but it would be nice...
 - https://github.com/dotnet/efcore/issues/14257 - Saving an Entity with an IPoint with Ordinates.XYZ in SpatiaLite throws an exception #14257 - reported in 2018 but still open...

3/6/2020
Started test project for the Created and Updated string - at first this seemed  contrived but as I did the first two tests I remembered that the number of cases here is actually worth making sure this is right - nice to get this started.

Changed the link list for slightly improved visual and better copy and paste.

Put in some 'ok' code to funnel preview mouse down from the ReadOnly TextBox to get list selection - it would be interesting to see if a mouse input gesture command would work and perhaps be more clear (right now selecting text selects the item - I think it would make more sense it selection triggered on click rather than mouse down?)

3/5/2020
The rendering with the WebView 'always on top' is in fact a known limitation of the control and I believe of XamlIslands in general - in the WPF/Winforms era a similiar issue went by the informal name of 'airspace'. At the time one interesting solution basically involved rendering the control into a 'native to the GUi tech' image when you needed to do something on top of it - I don't think that I will explore that and have other UI binding ideas on work arounds... This issue is doesn't have any immediate resolution but at least seems to track this https://github.com/dotnet/runtime/pull/33060

For local use I changed images to inline with Base64 and am copying in rather than trying to link the css. This is actually much nicer than I anticipated - in combo with the rendering in WebView this really imitates the site nicely.

In the WebView the scroll bars appear and cover content - as a simple work around added some margin in by default - at some point this could conflict with the existing CSS and I'm not doing anything to try to detect that but since I am not so sure adding margin to the body is something I am worried about overlapping with this seems like a good enough fix for now.

Add WebView to all current editors.

Add extract link command to all current editors.

3/4/2020
Inserted the WebView into the body editor to disappointing results - two concerns:
 - My first html for the local preview worked so I gave it very little scrutiny, so I was surprised when the images were broken in the first switch out. My formatting was less than perfect so I made some changes but still broken... The issue is documented here by Rick Strahl https://github.com/windows-toolkit/WindowsCommunityToolkit/issues/2211 and the issue was closed as 'by design', basically file:/// urls are not going to work without a IUriToStreamResolver - there is a note that a default implementation is available in a linked commit, but on a quick reading it isn't a public default but rather an internal. I agree with Strahl that this addition hoop is incredibly unwelcome in the context of WPF where you have full file system access...
  - Initial tests seem to show that there may be some Z order (or maybe just 'draw over') issues - I haven't fully verified that but in early tests it visually looks like familiar old problems...
  
So it is not like the old Web Browser control is genius - it is in fact painfully old and you have to work around issues too, but I had hoped for a more impressive start...

(While not used in this project in terms of 'unification/desktop modernization' effort into .NET 5 anyone working on desktop apps should take note of https://github.com/dotnet/runtime/pull/33060 - 'Support COM objects with dynamic keyword' - the inclusion of COM in .NET Core 3 has been painful because of the issue 'Dynamic keyword not working against COM objects' - Rick Strahl - https://github.com/dotnet/runtime/issues/12587 - from early responses for a pull to get so quick to merging so quickly is was a complete surprise to me!)

3/2/2020
I have made several tries at adding the newer MS WebView (soon to be WebView2) that uses an updated rendering engine with no success - Microsoft.Toolkit.Wpf.UI.Controls.WebView - in fact I have worked on sample projects and other experiments where I have also failed. In the early days I ran into code that would run but namespace issues that made everything seem broken. In recent attempts I was stopped because of various library issues including MVVMLight (EventToCommand) and and the related change from Microsoft.Xaml.Behaviors.Wpf to System.Windows.Interactivity (the Microsoft.Xaml.Behaviors.Wpf is the open source version of System.Windows.Interactivity) and my use of RelayCommand. WebView is still not in use in the current version but for the first time it installs without issue - details:
 - Switched to Microsoft.Xaml.Behaviors.Wpf - at first I thought this would require rewriting the MVVMLight EventToCommand but is seems so far to be simpler with InvokeCommandAction taking it's place
 - Switched the WPF FontAwesome package to a fork called FontAwesome5 after running into unknown but immediate breakage of the other package with WebView Installed
 - Leaned on the Refractored.MvvmHelpers for Command
 - Finally installed the WebView and was still able to run the app!

2/28/2020
Overwrite true/false support now in the Picture Resizing - this is only done by name, not hash/file size/contents/etc..., with the target really being adding resizing to the 'All Site Html Generation' not as much as perfect protection for changing a photo out on the file system and expecting magic to happen...

Added several methods to check/create the main Directories - this method should be useful when settings switching is added, much nicer to deal with generation if you know you at least tried to generate the folder structure to start.

Put the new methods together into the main form Generate All so that now pictures are fixed as needed.

Did 5 minutes of research on screen resolutions and changed the srcset size list now that it is easy to regenerate - quite slow to generate the image files but not concerned about that at this point and also a little unsure how much time is a result of the often quite large original photo size.

2/27/2020
First work on a slightly better Created and Updated - no test yet so leaving on the todo.

These two relate to better image backup and getting some reporting on missing images - with the html on the site regenerate all rewrites everything so checking for missing might be interesting but isn't currently necessary - images are currently only resized when you save and regen content - that needs to change to make sure all images are available (note that thankfully the site works on the available images - but it you only had the original in the folder that would be a terrible on site experience as it tried to possibly load 30megs of image for a 360px display...):
 - The photo editor on load looks for the original image in the media archive and if it doesn't find it it also looks in the current content directory and copies it back over.
 - The image resize gets a small refactor which makes it more flexible but most importantly it extracts the information that will be needed to evaluate if all the generated sizes are present in a content directory.


2/26/2020
JSON files are exported for each object right beside the html - this is meant as a backup of the raw data - you could argue that a better strategy would be to provide versioned db backups for each deploy - and I do think something like that would be great... But even an insanely well used db like SQLite immediately poses a problem for the lowest level tools because of drivers/format and because one goal of this project is low maintenance high durability that could sit for some time on the .net without attention - if you came back to a project 10 years later how sure are you that you could easily open the SQLite db? Compare to processing text... (Yes JSON has nuances to parsing, but I don't know of a text format to store data that avoids nuances? And working with text in the past I have found the terribleness of a particular format or variation is not nearly as terrible for a restricted set of input vs. the task of 'writing a JSON processor to handle ever use case properly'...).

Change in this version is to regenerate the the JSON on each HTML generation.

Better error when you open a photo but the expected Media Archive file isn't found (the 'original file') - originally I just shrugged and thought that with the Media Archive folder easy to backup that you just have to keep it backed up. Now I wonder if I should write original images back and forth into the on folders as needed - but like the JSON with the intent of keeping both?

2/25/2020
The first version of the RSS Feeds was all simple text with MS's SyndicationFeed and Item - quickly worked as expected out of the box. But when I went back to add images and html content I started having issues - part of the issue was certainly inexperience with the ins and outs of that API and now that I have finished out this project for now I do wonder if I circled back to the MS API if I could make it work with what I know now... On the other hand it seems like https://stackoverflow.com/questions/1118409/syndicationfeed-content-as-cdata and https://stackoverflow.com/questions/7204840/rss20feedformatter-ignores-textsyndicationcontent-type-for-syndicationitem-summa indicate that others have struggled with this issue also... Adding to my confusion is that while the RSS Feeds I looked at all seemed to be very consistent in using CDATA at least some RSS Spec example feeds and older info from Nick Bradbury (FeedDemon!) seem to suggest that the CDATA block isn't strictly needed to get an HTML description? So 100% admitting that some of this could be ignorance and confusion but basically:
 - I never found a 100% clean believable up to date seems to work in all readers I tried spec doc - a classic example of my confusion might be the sample file http://static.userland.com/gems/backend/rssTwoExample2.xml from  https://validator.w3.org/feed/docs/rss2.html - Newsblur shows no stories... Newsblur is touchy for sure (I had examples where Feedbin read the feed and Newsblur didn't) but it just needs to work so...
 - There were several work arounds in the StackOverflow questions - I had trouble with all of them and with the original code
So I decided just to go as simple as possible to try to eliminate as much misunderstanding as I could - two string builder methods and a little validation debugging later and it seems to be working as expected. As mentioned above - because I got this working so quickly this way and because of lack of a 'perfect sample' or reference and general confusion I do partly think some of the original solutions (either straight thru the ms api or stackoverflow workarounds) may have worked - but with this easy and working I don't really have any motivation to go back at this point!

Switched the Content/List feeds over to the RSS StringBuilder and refactored that code to a common location.

2/22/2020
Added an Extract New Links for Posts - this should probably expand and maybe become automatic to help you get all your links into the link lists - these are set by default not to appear in the Link RSS feed. Fair comment to note with these links already in content do they need to be in the link list - maybe not but for two reasons: saving links also saves to pinboard if you have an api key set (and this can be incredibly important for archiving) and because in the past I have definitely wanted to just quickly find 'that link' for someone (and while the 'All Content' list/simple search might fail with too much content the link list should be viable for quite some time since it is just divs/links/text...).

Added save and close and open link to the link editor.

Added Dublin Core style metadata to Content Pages.

2/21/2020
When I started looking at pulling all the json backup files in to recreate the database I realized that only their location in the directory structure defined what type was stored in the file (without analyzing the file anyway) - that might be 'ok' but I wanted a better way to tell the contents/type of the file so added more descriptive names. I considered creating a wrapper type that wrote type with nameof into the file but it seemed needless and harder to see than just changing the file names.

I like the auto-saving to Pinboard (most meaningful to an account with auto-archiving...) and thought it would be nice to extract the links from content and bring them up to save as links in the link list (so probably not fully auto at this point anyway) - to support that I added a field to the db structure to indicate whether the link should appear in the Link List RSS (so if you are following the site you are not bombarded twice by the link in a post and then the link in the link list).

I made another quick try at including the new WebView (soon WebView2) control - this time I avoided some initial problems but ended up rolling back because I still ran into System.Interactivity breaking - I believe I have a bit better understanding of which pieces would have to be switched out to use the System.Interactivity replacement but didn't have time to try so rolled back.

2/19/2020
Updated the Link List generation to alter the structures that are generated and to change some classes where I wanted the structure to be different than the compact content.
Changed the compact content generation so that the classes indicate when you have an 'optional' summary (has image - not imagining ever hiding the picture over the summary?) or a non-optional summary. This allowed a quick CSS change to get the desired effect which is good - but it did remind me that it would be interesting to look more at current best practices for css class strategies, I wonder if this should have a been an additional functional class (hide-for-mobile? or can-hide of the optional? or...) instead...

2/17/2020
Added Pinboard integration via a nice wrapper I found in Nuget/GitHub - notes:
 - In addition to 'liking' Pinboard integration is included because Pinboard has a plan where a copy of the of website at the time of the link is stashed - this is important for a long lived project because it is hard to predict what links and sites are going to disappear off the web...
 - A nice part of the Pinboard API is that 'add' has a flag for replace - for this integration to keep it simple I just call Add with the replace and don't worry about whether it existed before or not
 - Rather than separate actions for saving to Pinboard or UI choices I just decided to check for a Pinboard API Token - this could cause confusion if you setup a new config and didn't realize items weren't saving but I don't think that is a major concern at this point and I just put a progress line in.

2/16/2020
AngleSharp Notes:
 - Getting web pages thru the AngleSharp library was pleasantly easy - only hitch was that with the simple sample defaults one of the first pages I hit seemed to detect this style as 'noscript' and some of the metadata wasn't included (I am not even sure this was the intended behaviour, but regardless that was what was happening) but this was quickly fixed by including the AngleSharp.Js package and including .WithJs() - it could be this eventually causes an issue but pleasantly it didn't yet cause any issues or a long enough delay that I cared.
 - I have dealt with Open Graph metadata, and looked at other metadata, for HikeLemmon but hadn't before looked across sites for 'standard metadata' like author and publish date - at least with the sample of sites I looked at, which included many smaller news sites, it was a mess - og: data was often included (I assume in part for Facebook), some sites used https://www.dublincore.org/, some had twitter info, and some didn't have great info. I suppose I understand in terms of business, average consumer and lack of incentives - but this (plus the insane number of script tags included in basically all of these sites) makes me want to do a better job on this with this project - I feel like this data 'should' be there and consistent
