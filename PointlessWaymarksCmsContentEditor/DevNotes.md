## Todos
 - Clean up Temp Directory
 - Settings file switching or/and settings file selection on launch
 - Sorting needs better visual indicators
 - Folder Name in Lists needs to open in Explorer when clicked
 - Revisit og and dublin metadata - reference site not code and is it used correctly? Other tags that could be included?
 - Is everything getting HMTL Encoded - are there spots with leaks? (tests?)
 - RSS - Does title need CDATA to be completely safe? Or?
 - Basic Style for the Html Table output
 - Give all labels Targets
 - Figure out a system to allow StatusContext to help out postioning a new window vs the launch window

## Ideas
 - Provide a bit of abstraction to easily do MarkDown help text - see pdftocairo notes in settings - that works ok but font size doesn't match, link handler is code behind...
 - Photo Gallery - The current set of Content Types are proving in a few months of use to represent nicely what I want to store - but I am starting to wonder what the cost/benefit/opportunity might be to store photos beyond what is being used on the site and essentially have this as my photo site? Worried that cost may actually become an issue
 - 'Subsites' for years? When thinking about the photo gallery I started to wonder about the idea that you create in distinct units - so what if each 'year' of a site was essentially frozen in time, you reached the end of a 'unit' and you ended with a static website for the year that no longer 'needed' any updates to live for as long as you wanted - everything frozen... Maybe this idea is really best as buckets and subdomains and doesn't really relate to the app?
 - Top of the html page menu - or other 'nav' idea (maybe just a one level set of links that collapses on mobile? or...)
 - Top of the html page search box - maybe pass a query parameter to the all content list page?
 - Extract links from list page or 'all site' (option in content but not in lists/all)
 - What if you piped/setup your google alerts to RSS and there was integrated support for pulling them in and working with them. Obvious con is not sure even if RSS is still currently an option whether it will always be an option.
 - Backup the master media directory, database and current site to a dated folder (where?)
 - Could all font sizes be controlled by slider?
 - Detect image change and force over write style image generation
 - Explore https://css-ig.net/pingo - I wonder about quality and speed - I suppose this looses some flexibility but certainly current code is just resizing not watermarking/etc
 - Explore https://wkhtmltopdf.org/ - I this actually a jumping off point to an interlinked set of pdfs - maybe for some portion or subsection of the site

## Issues to Track
 - https://github.com/dotnet/wpf/issues/152 - Vaguely anyway tracks the issue where Xaml Islands render over all WPF content - not sure this is going anywhere but it would be nice...
 - https://github.com/dotnet/efcore/issues/14257 - Saving an Entity with an IPoint with Ordinates.XYZ in SpatiaLite throws an exception #14257 - reported in 2018 but still open...
 - https://github.com/dotnet/efcore/issues/14561 - Too many db operations and Spatialite crashes taking down entire program - in debug crashes the process with no information!

4/9/2020
Image and Photos can now start content from files and starting multiple files is supported - changes for this included option for an initial image in the editor and new code in the Action List.

Fixed a bug with the wrong year parsed from the 19xx photo name pattern - including this is a very personal choice but is such a productivity boost (and likely to not bother anyone else) that I think it is still work including.

Parse a Summary from photos from the title if the description is not present and add Exif description when present. While summary and title being nearly the same is not ideal the idea is to support/help quick photo content creation - basically provide as close to a complete piece of content from the image metadata import and don't have any barriers to providing more if the time and effort is there to do it.

Enhanced the autogeneration of the PDF Images so that an image editor launches with complete enough data to often be immediately saved.

Upgraded to .NET Core 3.1

4/8/2020
Added Jot - https://github.com/anakic/Jot and WpfScreen - https://github.com/micdenny/WpfScreenHelper - and looked at a WPF Sample - https://github.com/microsoft/WPF-Samples/tree/master/Windows/SaveWindowState - and eventually drew heavy and nearly direct inspriation from Markdown Monster to set up tracking and restoring window position - there are so many edge cases that the thought this is perfect is completely laughable but I did try to look at several sources for information and inspiration to quickly put something together that I hope is quite good without unexpected errors and minimal edge cases. As part of this added a manifest file to hopefully ensure the Dpi Aware support is specified by the app - these were the better links that I found but I am not sure I actually understand the when/why/what very well here esp. of what happens by default in what versions and what needs to be setup (probably need to play around with settings esp. when connected to an external monitor to do some real world experimenting and testing...) https://docs.microsoft.com/en-us/windows/communitytoolkit/controls/wpf-winforms/webview, https://github.com/dotnet/wpf/issues/859, https://github.com/microsoft/WPF-Samples/blob/master/PerMonitorDPI/readme.md

Fixed a binding bug that was two way to a read only property in the Link List.

Fixed missing save button command binding in Settings.

Fixed several copy and paste bugs related to the background data updates.

Initial pdftocairo pdf preview generation added.

4/5/2020
Fixed a bug in the Content saves for images and photos where I hadn't quite gotten the recent adjustments to saving/generating right - core detail is that in early versions the files were always an over write operation (vs checking) which meant I didn't want to wait for that everytime, but now the standard generate just checks for files rather than overwrites. Downside is if you switch out an image you have to force the regen...

Set up static weak events to serve as a way to alert/update the lists as content changes - light testing in the posts list and it was working nicely!

4/3/2020
Found while working on the Waterman Peak post that I was loading the content editor from the list item without refreshing from the database which had unintended consequences with the Waterman Peak post where I also added images, files and links so was bouncing between editors. Changed in all editors.

Even with the small current amount of content I was noticing that the list filter had some disappointing interaction where you would type and get a bad UI lag. Fixed this by removing the interaction trigger I was orginally using in favor of triggering off the property change so that I could also take advantage of the Dely binding options which causes WPF to delay before updating the binding (this seems like insanity but works really well letting you type and the binding not be updated until you pause - which makes the UI appear to be nicely responsive.)

Did some working on setting up all labels with targets - Settings screen done.

First try work on unhandled exceptions - got handlers setup and basic logging in place - need to see which ones I could 'handle', which in this case I think means report and try to resume as an unhandled exception is as likely to be 'error handling bug' as fatal error in the spirit of 'out of memory'. *Changed this up a bit after looking at the Markdown Monster code - this is not the result of extensive testing but rather my impression of the rather solid state of Markdown Monster.



4/1/2020
Added a font size slider to the Body Editor (for me if you make the window full screen the system font size is too small) - also made the refresh preview button a little larger and added a small gap between it and the preview to make it easier to hit.

Added an open file button to the file list to open the document locally - very convienent.

More progress messages especially around local HTML preview generation.

Small changes to the Status Message to give more room to error message display.

Bug Fixes Better handling of Image and File file checking.

3/27/2020
Added some basic progress messaging to the Body Preview construction.

Did a first simple round of work on pushing any navigation out of the WebView preview and into a new window - pretty simple code to start but at least for a the best case this works well.

3/23/2020
Fixed a bug in the Photo Content Editor in the new Media Library/Content Folder check/sync where I didn't have any path for 'first creation' where the file is not yet expected to exist.

Basic Editor visual improvements are done along with basic alt shortcuts - nothing stunning on either but already nice to have a slightly better look and also already using the shortcuts.

3/22/2020
I thought the switch to Spatialite had gone really nicely but had a mystery crash when almost done with the HTML import - https://github.com/dotnet/efcore/issues/14561 - this is a real show stopper where after some number of db operations Spatialite crashes and takes the process with it and when running in debug gives zero info back, I some quick fixes since I am not trying to squeeze performance - VACUUM, disposing and closing connections, pausing for seconds, gc collect... and nothing helped. Sadly for now I have disabled Spatialite and have fallen back to Sqlite. This is probably a wait and see at this point - this is a pretty severe bug so I hope it is worked on, but with so many outstanding issues who knows... One thought I had was to forgo doing any spatial db operations and store spatial data in Sqlite tables and then do operations in memory - this is certainly 2nd rate behind Spatialite but I am wondering if I am running into odd Spatialite issues here - and Sqlite issues nowhere - is it maybe better to do more work in code that I can more easily understand and control vs. relying on Spatialite working 'everywhere' (what would I do in an Android deploy - frankly it would work or not and I would have a difficult time debugging much less fixing any issues, sure same applies to Sqlite but I have had basically no problems so...)

Switched all Content Items to having a ContentVersion that is a UTC DateTime - easy to criticize that this is duplicate data to Created/Updated - I can't totally disagree but I want created and updated to be easy ways to access the human readable times without any hoop jumping related to timezone, so in the end went with the new ContentVersion field to store UTC time. Sqlite doesn't have RowVersion so UTC Time gives an easy unique-enough comparison field where it is also easy to see what was last in. Updated editors and JSON Import.

Json Import Working and used to pull in the current site! With ContentVersion incorporated this feels reasonable and solid - glad to have this option - durability over the long term is hard and while Sqlite is amazing everything goes wrong eventually so nice to have a way to restore from JSON files.

Did some more visual and shortcut key work - will have to work thru more editors and lists but it will be nice to have this in better shape. o intention of high design goals here - just simple improvements.

3/20/2020
Slightly improved the content list visuals - didn't do too much work pulling out resources or styles because I'm not sure at this point if 'more' content types are likely and if they are if they are going to conform well enough to the general pattern...

3/19/2020
In working on the Json import it became obvious it would be a benefit to have some logging. This was a fairly quick project because the GUI Commands and Feedback (and at this point nearly everything is a GUI executed command) run thru the StatusControlContext so code to write useful logging information really only had to go into that class to get a very useful start to ad hoc logging. The logging currently goes to a Sqlite database in the user docs folder. The most interesting detail that I haven't incorporated before is that each StatusControlContext gets a Guid id that is recorded as the 'sender' in the database - it could turn out to be more useful to log a thread id in terms of reconstruction but for now logging the Guid of the StatusControlContext is an easy way to get associated events.

JSON Imports are done with very light testing - T4 generation of the code did end up being a good strategy - easy to debug too. I was concerned about keeping a row version (3/13 log entry) and still think that might be a good idea (and I now think just a UTC DateTime would do it) but leaving that for now and just doing a very simple check on the created/updated fields (this won't help with any time zone type issues but should work nicely for simple 1 user predictable timezone uses).

3/17/2020
A very helpful link of git sparse checkout on windows - glad I came across information on this fairly quickly - https://stackoverflow.com/questions/23289006/on-windows-git-error-sparse-checkout-leaves-no-entry-on-the-working-directory - basically the problem appears to be that if you use powershell (I suspect all versions but wonder about the latest open source cross platform versions) and follow variations of the most common sparse check out recipes the echo [your sparse checkout dir] >> .git/info/sparse-checkout is actually producing unicode file with a BOM marker - instead you should do something like
 * echo "yourPath" | out-file -encoding ascii .git/info/sparse-checkout
 * Set-Content .git\info\sparse-checkout "someFolder/*" -Encoding Ascii

So a basic recipe of:
```
git init <repo>
cd <repo>
git remote add origin <url>
git config core.sparsecheckout true
Set-Content .git\info\sparse-checkout "SomeSubDir/*" -Encoding Ascii
git pull --depth=1 origin master
```

(Note I typed out this version because it makes sense to me atm - I believe there is now a 'sparse-checkout' command that is at least experimental - there may also be some good details with --filter but I neither immediately understood nor dug into that.)


3/13/2020
Switched DB over to SQLite/Spatialite.

Did some basic code to detect needed but blank settings values and fill them with defaults - this works for now but there is no current way to switch settings file and I do think that will need to be added eventually - current initialization done in the App.xaml constructor.

Wired up the GUI Import and did a first partial import before it broke on the Link imports - it did some imports successfully. Leaving the current commit as a WIP with this broken in part to think about whether I want to change the design to not duplicate anything if you import again. Would the best strategy actually be to move to a row GUID - the current ContentGuid is only a way of connecting the same content - not a way to identify a individual db entity (row...)... This seems like overkill but I wonder if it might be quite interesting for working with a temp local version and bringing over all new content? But you still end up with 'which version is the latest', so is it even worth it?

3/12/2020
Basic Json Import Methods are done - completely untested.

3/11/2020
Changed the model interfaces to be read only - there is some potential lost flexibility in this change but at this point I am comfortable with the 'rule' that you need to use the concrete type if you want to modify the database.

The interface change was largely a realization from working on the json import for the Notes - notes is really the only type that didn't implement all the so-far-needed data interfaces (has a generated title rather than a full 'user' title) which means it is an 'exception'... But by using the NotMapped attribute I was able to added the needed Title and MainPicture properties without storing them in the database (which in retrospect might have been a workable idea - but with this in place I am not going to revisit that at the moment) - this is nice because note now can implement all needed interfaces without any outside mapping needed. 

More Json import generation - historic imports now building - like all json imports 100% untested at the moment.

3/10/2020
Added the first Json imports - some via T4 generated code and Notes as handwritten. Building but not tested.

Added a style for the outer border of the Lists - this is designed to make selected 'obvious enough' without needing to do any indicators.

3/8/2020
Switched some items over to direct https reference - this is 'right' in so many ways but my concern is that I actually don't 100% care about public internet availability for this project - https makes sense for the modern web but I don't want to get in the way of serving a website locally from something like a raspberry pi - I haven't  experimented with this yet but I am starting to be more interested in what in more limited, maybe personal, content - what if a site was only available from my backpack, or my truck - what if a site could truly be turned off, maybe a Mondays only site, not meaning a clever splash screen on other days but literally off - what about a site from your desk literally only running when you were in. I don't know, you can easily think of a million reasons 'not this' or 'it doesn't actually make sense' - but I am curious.

Finished out current Created and Updated Testing.

Better options in the 'General' tab to generate HTML without the Picture Check/Clean and pulled that into a button also.

Added checking in the Link List that selected items are on Pinboard - just checking the URLs not syncing data, but I think the most important is that the URL has been saved since the most core idea is to sync to Pinboard for archiving.

Html Encoding added for quite a bit of meta data.

Added a wpf Behavior to funnel PreviewMouseDown in Readonly Text Boxes to the containing list item for selection - not quite 'perfect' (should perhaps still look at changing this to click) but cleaner than a pure event and at least here defaults to 'more selected' rather than 'less selected' (and better here because unlike like the magic invisible boundaries that defined selection clicks before you can at least understand that selecting text selects the item).

Slight visual improvement in the Link List for selection with just a small margin adjustment.

I had been testing by using dotnet serve - trying to be modern and cool but it turns out that good old iis was setup with zero hassle in about 2 minutes for a simple static site like this - so moving over to that. It would be awesome if this project tested at the push of a button but I think it is better to just use iis (or server of choice) and use time elsewhere.

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
