## Todos
 - To Excel needs some formatting for Body Content and Maybe an option to Exclude it?
 - Content Testing:
  - Ironwood Integration Tests for Context File Add, Context Image Add, Post, Note, Link
  - Add tests for the common content validation
 - Rework the Daily Photos generation so specific days can be build
 - Gui Validation alerts - Title control and Tags Control Done
 - Where to integrate the Excel Import to make it more obvious to find? Title bar all lists?
 - Try upgrading EF to preview and using the Collate function for the Link 'does url already exist' check
 - A bad content code should be handled better
 - Bad Content Code Content Scan
 - To Excel for logs
 - Deleted Content Report so it is possible to restore completely deleted
 - The Changed Html generation doesn't detect changes to settings that should trigger a full generation (but need to avoid writing the settings into the database - keeping secrets out of the db means it can be pushed to the public site as a 'backup' strategy)
 - Refactor the Email HTML so you code send any current type to it
    - Look at setting this up so that you could also use this to create a custom one off email to someone with content? So if someone asked a question you could go to your email client, type a short message and then paste in the content block (only a modest gain over sending a link but there is some value in 'last mile' convenience) - I suspect the detail here is getting the html to the clipboard correctly...
 - In Search it might be nice to have the content type on the line with date?
 - Clean up the main window - split out context - consider creating a control?
 - Sorting needs better visual indicators
 - Folder Name in Lists needs to open in Explorer when clicked
 - Folder in editor should help you with existing choices
 - Revisit og and dublin metadata - reference site not code and is it used correctly? Other tags that could be included?
 - Is everything getting HMTL Encoded - are there spots with leaks? (tests?)
 - RSS - Does title need CDATA to be completely safe? Or?
 - Figure out a system to allow StatusContext to help out positioning a new window vs the launch window

## Ideas
 - GUI Automation Testing https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/march/test-run-automating-ui-tests-in-wpf-applications and/or ViewModel testing (the ViewModels should be testable without the views - however an interesting issue is that testing the GUI will test both...)
 - Look at deployment options - self contained? msix? automated?
 - Watch https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/ - the source generators look like they could be quite interesting for INPC and HasChanges
 - Provide a bit of abstraction to easily do MarkDown help text - see pdftocairo notes in settings - that works ok but font size don't match, link handler is code behind...
 - Could all font sizes be controlled by slider? I like the control in the editor but maybe everywhere would be more useful? And persist in Settings?
 - Explore https://wkhtmltopdf.org/ - Is this actually a jumping off point to an interlinked set of pdfs - maybe for some portion or subsection of the site - or maybe look for other PDF generation strategies
 - How hard would it be to create a GridSplitter that would be easy to set the initial split based off of the last editor use - and/or that could completely hide content to one side with a shortcut
 
## Issues to Track
 - https://github.com/dotnet/wpf/issues/152 - Vaguely anyway tracks the issue where Xaml Islands render over all WPF content - not sure this is going anywhere but it would be nice...
 - https://github.com/dotnet/efcore/issues/14561 - Too many db operations and Spatialite crashes taking down entire program - in debug crashes the process with no information!

## Projects to Review
 - https://github.com/statiqdev/Statiq.Framework - found Wyam (the older version of this) accidentally thru an older Scott Hanselman post https://www.hanselman.com/blog/ExploringWyamANETStaticSiteContentGenerator.aspx and thought it might be worth review - I haven't looked at too much static site generation code so this could be useful.

## Notes

7/28/2020

When I added the model for LinkStreams I didn't want to call it 'Content' because the links don't get a 'content page' they are always intended as just a list - at the time this difference seemed huge and important - over time that concern faded as everything is in a list and as more operations work over all 'content types' and it became somewhat mentally offputting to have this naming exception.

First version of the DataNotification support in the tags list - light manual testing suggests this is working nicely!

In looking at the DataNotification support for the tags list I made a bug fix - errors were falling thru rather than returning - and improvement - data notification subscriptions detach before load and attach after load to minimize the overlap of working on the data via notifications and loading overlapping - to the list data notifications.

7/27/2020

Realized that the 'Changed' only generation was missing detection for content that has been deleted when determining whether to regenerate photo gallery and list content. This triggered several changes:
 - Added methods to the DB class for deleting content from the DB and refactored the lists to use this - previously the delete routines were in the list contexts, this makes it easier to reuse the methods (for testing at this point)
 - The list delete refactoring enables multi item delete in all lists, improves the multi-item delete message and moves the responsibility of removing items from the list from the delete method to the data notifications
 - Added DB Methods to get the last historic entry from any 'completely deleted' items, ie historic items where the content id no longer exists in the content table, and used these methods to help determine what lists and galleries to generate in the Changed Html methods.
 - Added a basic photo test for delete, get deleted and re-save.

7/25/2020

While working on the Image Testing ran into a situation where it appears between the .net, sqlite and json datetime representations that, at least as I was handling everything, some precision was lost in the ContentVersion. ContentVersion is now truncated to seconds on the DB Save - the precision simply isn't needed and between all formats it seems as if seconds is handled without any additional work which is a huge bonus.

7/23/2020

Fixed a bug in the Excel Import where Content Version was not being set causing the Change Detecting import to not see all the changes - fixed by moving the Content Version assignment into the db save routines and removing other places they are assigned.

Fixed the Status Control on the Tag page that wasn't spanning all rows. Improved the changed message for the Excel Import - the change to the friendly printing reporting was nice but a newline was missing between the title and changes.

The Tag List UI is now in a more reasonable place. For a general purpose application the current design is easily criticized as not giving intermediate users good options - for beginners the easy option to edit each content item for a tag is probably best - and for advanced users it is hard beat spreadsheet style editing (Lightroom might give an interesting model here with the way it *s tags in multiple photos - but I don't think that is actually as good as editing in Excel even though it is very workable for advanced users in a compact space that doesn't require moving away and back into lightroom... And worth noting in Lightroom for confusing/messy/problem edits I think many people at some point end up wanting to edit and reimport from a spreadsheet...). But best for intermediate would be some editing tools to rename/delete/merge tags in the UI - leaving out because of a combination of the complexity of doing this well and the extremely high utility of the Excel import (fwiw I wouldn't rate this as the best 'beginner' interface, I think there is just too much possible confusion of what is changing where and the consequences of details like merging or splitting tags even with a really beautiful interface that guides/warns, provides undo/redo, gives a great display of what is changing).

Take advantage of the 'changed' information and add a few more filters to generating only changes - Daily Photos (which were originally designed with 'interlocking' previous and next pages for generation) and Tags (where detecting changes is not simple) are still 'full' generations for now.

7/22/2020

Overall 'Generate All' time is not currently a concern - it is in set the computer down and get a coffee range which is essentially on target for my goals for this project. What is getting tougher with  2,000+ photos is the sync to Amazon S3. One issue might be that I am currently using BeyondCompare - it may not be the ideal tool because of the slow scan of S3 for changes and what seems to be single thread upload to Amazon - but regardless of tool choice (and short of custom tooling) with any tool one item that is currently becoming and issue is that the All generation touches every html and JSON file adding up an impressive list of changes on items that likely didn't change causing way more delay updating the site on S3 and dwarfing the time to generate.

To address that this update detects changed content and related content to give a 'generate changes' option. This could be taken farther as the Daily Photos, Lists, Tags, etc. still always regenerate but I think this is a good starting spot.
 - Routines to extract bracket codes and main image and write a related content table - intent here is this is wiped and regenerated every use
 - Started writing the last generation time into settings
 - Routines to detect changed and related content and a db table to hold that info to easily join content against in getting content to update

 Related to this the Main window had quite a bit of key HTML generation code in it - refactored that back into the data class and did a light rework of the related tabs.

7/21/2020

Changed the Excel import differences string to a format suggested by https://github.com/GregFinzer/Compare-Net-Objects/wiki/User-Friendly-Report.

Added methods to purge content not in the db from both generated content folders and the Media Archives - made these accessible in the Main form but at this point did not integrate them into the 'generate all' process, still wondering if that is the way to go...

Updated the data notifications response codes in the list context to the better form found in the Photo list - the idea here is to basically distrust current state and message order and regardless of whether the send says update or new check and add it only if not already in the list and update in either case.

7/20/2020

It was great to get the Excel Import integration test working early in the process but today's changes are a result of that hitting me doing 'actual real world' importing and it produced a lot of changes.
 - The Excel import wasn't validating content until trying to save - now it validates it before returning info on the import.
 - The import last updated by is now excluded from the object comparison to avoid falsely registering a difference when the only change is the updated by - this could be more sophisticated and does leave a hole where if you were really trying to update only the last updated that it would be frustrating but I think this is good enough for now.
 - Improved the Excel import progress - still chaotic for sure but better than showing nothing
 - Imports with files were set to regenerate the files - changed, that doesn't make sense since the selected file is not going to update in the import
 - In working with an import from excel the high precision times that EF was storing were triggering changes as ClosedXML was picking up only readable times (to the second) from the default format. In general I don't like loosing time precision but in thinking about 'by-hand' editing the full precision is not helpful. Added a DateTime helper that reflects over an object and truncates DateTimes to seconds - I guess this could play havoc with the apparent ordering of high speed photos but that is a highly imaginary concern at this point.
 - The Excel imports were partly inspired by some Photo clean up that I needed to do - in working on that I found tags that didn't meet the current tagging limitations on characters - ultimately I decided to make the tag clean up more aggressive. This could create some frustration with not being able to fully enter what you want but should help keep the tags clean in the way I currently am thinking about them.

Ran into an interesting issue with the Status Context dialog - I was using Task.Delay to block for 3 minutes so that the program didn't get 'completely stuck' in a dialog - I think that concern resonated most strongly when I was first working on and having trouble with setting everything up correctly on the dialogs... I finally let a dialog 'expire' and an import started that I wasn't actually ready to run yet. After some searching added a WhenCancelled extension method for the cancellation token to make it easy to wait as long as needed.

Started the first bit of rework on the tag list - changed it into a true listbox and added Excel Export (added a supporting class the implements ICommonContent as an easy way of getting most fields for most content types all together in one list) so you can work in Excel and import tag changes to multiple content types at once.

7/19/2020

Excel Photo Update import test is passing.

Validation improvements and fixes including a model Interface simplification.

Added to the Generator Save methods a routine that uses reflection to run null safe trim to empty string on any incoming string fields - that plus a tag cleanup should produce a cleaner more standard db input.

Added a pinned dispatcher property to ThreadSwitcher to facilitate setting up and using a consistent dispatcher in situations like tests where Application.Current.Dispatcher is not going to work.

Working photo context test! Haven't test a backing class like this before - still doesn't tell you if it is actually wired correctly to the UI (and as noted maybe testing the GUI is the way to go - ie if it isn't wired up and displaying correctly does it matter it is technically correct?) but very interesting to get this working.

Failing Excel Import/Export test added - not sure what is failing but happy to get this into testing!!!

7/18/2020

Excel Import - the first Excel import code was photo content specific - currently I have more photo content than anything wlse on PointlessWaymarks.com so that was the most benefit. But in working on the code it became obvious that it was going to be reasonable and possible to design one import to handle the current content types - so over a few commits put together code to do the imports. Only 'first ad hoc tested'.

7/15/2020

Consolidated some validation into CommonContent Validation.

Fixed a bug in saving to make sure that the content editors don't reload data if generation fails (which could both leave you in a bad state and potentially wipe out your work or error trying to load null).

The TitleSummarySlug and Tags editor now have validation error and warnings (only warning is in tags) for some validation (the slug is unique validation is not run at the moment to avoid issues with performance and/or more complicated issues of caching/delay bindings/etc). Working on this triggered a number of changes tightening up validations and some additional tests.

Removed ObservableRangeCollection - this has been great for me in XamarinForms but I ran into some issues in WPF, potentially fixable but as with every time I have tried extensions of ObeservableCollection in the past it seems easier just to deal with the threading than work around...

Removed the last of the older Event Notification code that is now switched to TinyIpc and at the same time move the Image Content Editor over to the ImageGeneration.

Rewrote the DataNotification code in Photos - basically I had realized in previous work that the update code I initially wrote assumed 100% in order no error transmission and event creation from the sender - which even for small single app messaging seems improbable to be always true so code is rewritten to honor delete vs update/create but in all cases to check first for the GUI contents and update as needed. I anticipate all the lists moving to this style but for now testing in Photos.

7/14/2020

Used https://github.com/cyotek/SimpleScreenshotCapture/ as the basis for modified code to capture window images via native methods so that you can capture both WPF content (as you would with any WPF technique for images of windows/controls) and XamlIsland type controls also (which are rendered differently and don't show up at least with the WPF techniques I have tried).

Finished out the conversion from FontAwesome to XAML/SVG Icons - more work than expected overall setting up the user control and switching out icons everywhere but I think this is a good trade (especially since so far the first version of the UserControl seems to be holding up for both icon and in button use) - of course it is not reasonable for this project to not take dependencies, but I think in the case of FontAwesome it is unreasonable to believe I could pin the current version and work for years without anything breaking, I do want this project to stay 'up to date' - but I also have ambitions for this being something that I might still use in 5-10 years, overall Windows Desktop software has proved remarkably durable and tooling from MS very supportive of older frameworks/projects (this is based on experience at work where things have run for many years between edits...) so I think this might be reasonable and but I don't think it is reasonable for FontAwesome which was a core-non-optional part of the GUI.

Fixed a bug in the Photo List where reloading wasn't properly applying sort and filter.

7/13/2020

More work on the newer test setup - this moves the test coverage backwards temporarily but I think the refactoring/restructuring to the tests will pay off - good first start on refactoring Photos.

While working on the image processing I accidentally found that the System.Drawing out of memory exception I had gotten on some images (which it is well known can be a format or other issue and the out of memory is not an accurate error) was potentially caused by a long file name bug. I originally thought it might be long metadata title - but I got a more informative error by eliminating this use of System.Drawing and using Photosauce Magicscaler instead. Magicscaler returned a Directory Not Found exception, but the fileinfo object generating the filename reports 'exists' and I could navigate to the directory and file. Some research turned up long filename related bugs - https://stackoverflow.com/questions/5188527/how-to-deal-with-files-with-a-name-longer-than-259-characters - and the extended path length prefix. Passing file names with that prefix to Magicscaler fixed the issue.

Replaced FontAwesome in the TitleSummarySlug editor - this turned out to be more difficult than I imagined with tooltip display and flexible sizing, to get that I ended up with a UserControl that seems overly complex for what I want to accomplish but does currently seem to meet the goals...

7/12/2020

Thinking some about writing more integration tests it occurred to me that it might be more useful to build something 'coherent' so that making it more complex made sense and so that the result would make more sense for visual inspection and perhaps as a decent testbed for css and new features. To that end spent time this AM getting a few Ironwood Forest National Monument pictures and PDFs setup to start with - this will mean time reworking some of the existing testing, but I think it will be worth it to have a way to build a 'more real' test site.

Added the html photo metadata report to the reports menu in lists after a small refactor on the report code in the photo editor - finding it quite useful to get the metadata into a format that you I can search without an additional step.

In the photo metadata extraction added Taken By and License fall backs to xmp and iptc.

7/9/2020

Reworked the organization on the data project - I'm not 100% confident that this matters or is 'right' but it does feel immediately more reasonable.
  - The T4 templates were the biggest problem - on the eap version of resharper I am currently using the ForTea plugin doesn't seem available and I wonder if there is any chance it could have helped? At least the generated content gives good errors to work from - but I wonder if it might be time to look again at Razor templates, last time I looked it seemed possible but also messy.

7/8/2020

Changed out Photo and Image types to have Body Content - my original thought was that a Photo with Body Content should be a post, that still strikes me as reasonable but recent images like the Roskruge map made me think about how some images are 'pure images' and some are 'resources' where the page for the item should present more information as needed. I had already started down this path a bit with the 'Image Source Notes' field for images, I think this just takes it out to a logical conclusion. At this point too based on the use of files I am more comfortable with 'views' of a file/image/photo that does give you the Body text and don't feel the need to incorporate it in every view...

Had a slightly unpleasant experience with FluentMigrator where Rename and Delete columns were silently not running in migrations - this is familiar at this point as Sqlite has traditionally not supported some operations that other DBs do and working around those limits for all situations is challenging (I think Sqlite recently added column rename support so maybe this changes in the future! There is some basic explanation of the issue here at the bottom about why https://www.sqlite.org/lang_altertable.html). Changed the migration over to a sql script - because I have kept the db setup simple the script to do the changes is verbose because of all the columns but conceptually very simple and fluent migrator does make it easy to run.

While working on the migrations found that I didn't have the correct matching columns in the HistoricImage type - fixed - I wonder if I should revisit how this code is written and explore using inheritance to ensure that the types are always in sync. In the past I have had trouble with inheritance and EF not acting always as expected but probably time to revisit that and see both what comes up now and what I can do with some of the ignore options.

Removed more old code.

Fixed bugs introduced in the FileGenerator work.

A little more StatusControl appearance and layout work.

7/7/2020

Basically while adding resources for https://pointlesswaymarks.com/Posts/2020/empirita-ranch-6-18-2020/empirita-ranch-6-18-2020.html I found that in some cases I want images to do some of the things that files do because they are serving the same purpose in some cases like the Roskruge 1893 Map of Pima County - resulted in three things:
 - Bracket codes to hyperlink to an image rather than showing the image - added for photos also
 - Added a command to open an Image from the Image List.
 - Added a todo to update images to be more like Files in terms of content setup

Small changes to the StatusControl - thinking about changes for better appearance and layout.

Bug fixes for Tags (wrong value) and Index (wrong method called for type).

7/6/2020

Added a File Generator and Integration tests.

Cleaned up a few compiler warnings.

Removed old code.

7/5/2020

This app is all about a single power user working on the desktop against a local Sqlite db - given that scenario two problems that come up:

Problem 1 - Cross App Data Change Notifications:
Because lists of things and editing those things is a very common pattern there quickly becomes a need to update the list as edits are saved in another window. Previously in this app I did that via a centralized class and Weak Events (thomaslevesque/WeakEvent: Generic weak event implementation - https://github.com/thomaslevesque/WeakEvent/ ) - MVVM Light Messenger is representative of an another  pattern that can also be used and either solves this problem rather nicely.

Problem 2 - Interprocess Data Change Notifications:
In some cases solving problem 1 is all an app will ever need, especially if you put a single instance constraint on your app. But I have found that if you have 'power users' that may launch multiple apps or have a scenario where multiple apps (or the app and a long running background 'script' app) work on the same data (and likely share common code to do that - say both an Inventory Management and Purchase Order app that both can show and edit Vendor Information) you quickly end up with a real scenario for Interprocess communication. Searching for Interprocess Communication for windows desktop apps devolves into many solutions that could work - recently I found a new library that I had not seen before that seems to work in initial experiments and takes care of an important part of dealing with this problem which is nicely wrapping up the native windows calls into something nice to work with in .NET - TinyIpc https://github.com/steamcore/TinyIpc - from the GitHub pages: ".NET inter process broadcast message bus. Intended for quick broadcast messaging in Windows desktop applications, it just works."

While at the moment Interprocess updates is not an urgent need I did go ahead and update to TinyIpc because I want this for myself and I am especially intrigued with the idea for me (or maybe super power users) that you could do things via a cli and have the updates available in a GUI running at the same time (without network, polling for changes or having a db that can broadcast notification).

7/2/2020

Changed the Photo editor and photo automated import over to use the new Photo Generation - resulted in a few changes in the Photo Generation to accommodate needs in the GUI code but was generally smooth. The interesting thing about doing this is that even though the base methods were extracted and are now tested (good!) there is still quite a few details to get everything correct in the GUI - I'm sure that the code could always be structured better to minimize concerns but I also suspect that long term some UI Automation testing could pay off - adding it to the todo.

Fixed a small layout error in the Main Window General Tab.

Started work on a routine to clean/purge the folder structure - the motivation here is that the older GUI Photo save routines detected and moved folders/files as possible but for simplicity the newer photo routines do not do this so another cleanup strategy is needed.

7/1/2020

More work on the Photo Import Testing - it could of course be more comprehensive (probably most missing is some validation of the html - or maybe checking specific elements?) but I think this is a great first 'safety' sanity check to get in place and should help the project to move in this direction for all content types.

Updated the data notifications in the Lists to repspond to the newer local content update notifications - also a small refactor to more easily get the gui image url without getting tripped up on nulls or other invalid values (of course truly invalid data is very important - but not here, these gui routines should display what is possible to display and as much as possible not crash due to a missing picture/invalid data).

In the photo testing for the JSON data files I wanted to compare all properties and found CompareNETObjects to be very useful - https://github.com/GregFinzer/Compare-Net-Objects!

6/30/2020

Have a working single working test of the Photo import with asserts for the imported metadata! The test could probably be setup in a more useful way but for now this seems like it is a great jumping off point to getting more tests around the Photo Generation and then use this style to test other generations.

6/29/2020

Continued work on the Photo Generator code that is basically a refactor of the GUI Photo Editor code - got to a point that seems complete enough to put in a test, but had a quick first failure on the test because it appears I am building the basic site structure but not setting up the db... Some details that came up working on the Photo Generator:
 - Added a method to delete image files in a Photo folder where the image is not related to the current original image - this is essentially more brute force than the previous GUI version but I think is the best because it doesn't require knowing before/after details rather just acts on 'current'
 - While working with the data notifications and thinking about issues in the past it occurred to me that it might be an interesting approach to notify both on db changes (so the latest text/entries/etc is displayed) but also on 'local content' changed, since some GUI elements use images/generated content for display separating these two might allow the GUI to keep up in a nice way.
 - Made a 'for now anyway' change to validate each file name so that URL escaping is not needed for content URLs - it might be better to instead make sure that URLs to this content is always appropriately escaped but for now I am interested in minimal hassle and as much parity between the local files and site files as possible.

6/28/2020

Jumping around a little to work on some tests - now have a start on creating a test site in a nunit OneTimeSetup, added a test that checks that the very top level folders were create and added two content files to the test project for future use. Working on the methods that check that media files are both in the media folder and the content folder made me think about how easy it would be to make a mistake that types/compiler would not catch - hoping this is a good start to getting some tests in place on all the file and directory creation.

6/27/2020

Expanded some on the GenerationReturn work - I like some of the details although I am slightly torn over whether this should be 'just' a log type, ie is it worth having log entries and the Generation Returns that also write to the log? Generation is such a special case I think this is 'worth it' but it is frankly a little hard to say.

6/24/2020

Did some work to start trying to refactor the site generation around what I know now after working with the system - the main two items this go were to start building methods with a Generation Return object that allow better information return considering that one of the needed flows is to log an error, keep going and then pass the error information along. I don't think this is in it's final form yet but I think the current object and the methods around confirming all the folder structure and base media files are setup are a start.

6/22/2020

When I first found FontAwesome for WPF I was really thrilled - but a recent library update broke this functionality, could have been my usage or bad luck or ... and I was able to role back to an earlier library without issue - but I think in retrospect that what I should do instead is to use icons like those from http://modernuiicons.com/ and use a control for the 'wait' spinner. This version starts the process with a spinner control from http://blog.trustmycode.net/?p=133 and https://github.com/ThomasSteinbinder/WPFAnimatedLoadingSpinner - I believe this will be more durable in the long term and especially with the extreme durability, at least in the past, of MS Desktop technologies there is a benefit to leaning in when reasonable to longer lasting approaches.

This is the first generation of the site that I ran into an issue where a page had a bracket code to a non-existent Content ID - I added some guard code, changed the generation ordering and added some code around checking for and generating missing content but this need more attention both potentially in 'pre-checks' and/or in making sure that in as many cases as possible generation can continue and a good error report is generated.

6/17/2020

The photo imports previously tried to process all items at the same time with a number of files under 5 - but today had an error related to a daily photos related content thumbnail and generation that I believe would have been prevented by the sequential style imports so switched over to only that.

More Tag display/edit work including some refactoring on the Save code.

Nuget Update.

6/15/2020

Changed the way the Photo Reports work - initially I set up a Expression so that a new db query could be issued with it - this approach is very clean for the 'no tags' report but in the Title Date and Photo Created Date mismatch report I needed to parse the Title to both check for and extract the year and month, certainly possible in pure sql but easier to just process all the entries in C# code - so the report function now generates the list rather than being an expression for filter and EF Query.

In working with the mismatched Title and Photo Created dates realized that when deleting I was writing a last historic entry (good) but leaving no record of the date of the deletion - changed the list deletes to have the deleted date as the last updated date, this is a slight bend of the way things worked before where historic was really only ever a pure copy but this records the data nicely and it isn't hard to argue deleting the entry is a type of update...

Fixed a missing command on the newer button to flip between load all and load recent in the Photos.

In the photo import added some logic to strip out multiple white spaces - this is slightly questionable but I felt like in bulk photo updates this was a slight advantage in getting normalized titles - but on the other hand if you had a common in your title and wanted two spaces after it that is pretty reasonable? Not quite sure here...

6/14/2020

No Tags report implemented - first use of the menu bar in the app - there may be enough actions and future planned actions with the reports that it might be worth it for organization now.

6/10/2020

Delete in photos was loading after the delete finished - this was an easy idea in the beginning but was essentially a bug after the addition of recent/all - changed.

Make the Photo Load Mode more sophisticated to setup for using this control for a photos report window - because Photos lean heavily on imports rather than entering all the info it is pretty easy for unintended items to slip in - a good example might be 'photos with no tags' - not necessarily an 'error' but at least in my usage unintended and worth reporting on, another example since I heavily use a date prefix that the program processes a mismatch between the taken date and the title date and missing license or copyright...

6/8/2020

First version of the Menu Editor is in place and the generation is now using this - nicely simple since the current menu implementation is just a flexbox of links - originally I thought about reworking this to something 'more' but this style has grown on me, still needs consideration for mobile but... Added basic help.

Fixed a bug in moving folders in posts where a new parent folder wasn't created when needed.

First use of the Special Page bracket codes in the Subscribe page - worked well. Updated the help to the bracket codes to reflect this.

6/6/2020

Setting up for a MenuLinks table so that the current 'core links' is database driven. This should be a nice step towards being able to quickly generate other sites.

The emerging most popular use for images is to show content in files esp. via the pdf image extraction - this is great/as intended but an aspect I didn't anticipate is that many of these images essentially create 'duplicates' in the search results that are not helpful and take away from finding the file/better content (the file and the image are likely to have the same thumbnail...) - added a ShowInSearch column to the Image table to be able to control this. Most interesting issue with adding this was that an image excluded from search could have a unique tag - so tag pages are generated for all tags but the tag search page only gets tags from pages included in search.

6/5/2020

While saving a Pima County 1999 Mountain Parks report I found I needed to rotate an image - normally with a photo this is corrected before it hits this program since the current main photo source is Lightroom - however in this case because this was an extracted image from a pdf and this program is actually the quickest way for me to do that it was obvious that rotation in the UI would be a benefit - thankfully an easy change by combing MagicScaler and the recent change to the way the editor image/photo is displayed.

Taking memory usage off the todo list - the UI changes plus the switch to MagicScaler seem to have at least improved the situation enough to let it go for now.

Via a little string manipulation to replace - with spaces - this could eventually need data structure changes but for now this works for the display of tags.

Bracket Codes extended to support 'Special Pages' like {{index}}, {{indexrss}}, {{searchpage}}, etc... In the process put some tests into place on the Bracket Code processing and improved that code! This is partly for general use and partly for the upcoming MenuLinks table where bracket codes will be valid and as a default setup using the bracket codes creates one more spot that where changes to this code or a site should be less likely to break links.

6/4/2020

Switched the content lists over to 'Item' caching and scrolling - while the scrolling is not as nice as before the performance impact on Loading the Photo list was absurd - where before it would pause for ?1 minute or more? to rebind the ObservableCollection with 1000 or so entries now it loads in <10 seconds (which for the functionality of an 'all' list without paging seems worth it to me for the moment).

Remove ImageSharp in favor of MagicScaler. With use of simple/minimal code MagicScaler is notably faster and I appreciate the switch to an Ms-Pl licensed library.

Add a more aggressive image clean up setting to better clear out old/erroneous images.

6/3/2020

Switched the main resizing over to MagicScaler.

Beginning to look at memory usage a little more -
 - ImageSharp appears to be holding quite a bit of memory in some cases although I am not sure what the expected GC pattern is here so I am not sure this is a 'problem'
 - I am still suspicious of the Photo Editor form based largely on watching memory (which I am completely aware is 'anecodotal' evidence at the best, I absolutely don't have internals knowledge to let me predict what the GC is going to decide...) - in looking at the code I did identify the opportunity to rescale the selected image in memory for delay on the form rather than referencing and downscaling the full sized image - there is some up front cost but this appears to be a win. As part of this read a number of posts around a Google search in the spirit of 'wpf bitmap memory leak' which are not encouraging and also many of the results are old and there is at least some chance WPF internals have changed.

6/2/2020

I had an out of memory error today after some heavy photo loading - a super quick check of the default Diagnostics view in VS suggests that I am not freeing something related to photos and memory usage goes crazy by the end of a large export - nice that the program has been staying responsive, but I need to track down the memory leak...

On HikeLemmon email subscription popularity was a surprise for me so I wanted to find a way to do it in here. So far:
 - Rather than a custom page I just did an iframe in a post page - this will never be custom beautiful, and another more beautiful approach might have been a custom page that abstracts the typical sign up process, but it seems highly functional and practical and I suspect means minimal maintenance and no additional credential storage or other abstractions.
 - Added an 'HTML Email to clipboard' function and supporting methods - right now anyway 'posts' are very simple so only does some minimal common email formatting to get a simple email together. One challenge is that my simple posts are 'by convention' and a Post can actually contain anything markup legal (so basically anything) and email HTML compatibility is very terrible... However this solves the actual current problem in an acceptable way.
 - Signed up for SendInBlue and connected google reCaptcha - their free plan seems to currently cover all current needs, first test went well and the API docs look encouraging (important since a 'publish' action rather than HTML to the clipboard is the eventual plan).

Caught a bug in the bracket code refactor of images - fixed.

5/31/2020

Added a table and migration to hold pairs of related content - this is not wired up/processed yet but the idea is that this table will track related content (which otherwise is largely trapped in the text of a post in bracket codes) and allow generation of a change list for more efficient html generation and perhaps a list of file changes.

To setup for tracking related content the Bracket Code was refactored - both to have an easy way to get the related content and also to pull out some common code.

Added a migration for the new related content table.

5/21/2020

Supplement the title with the file name if the title in both XMP and IPTC title are blank - originally I thought this was a mistake because it can leave you with Titles that are whatever file name format your camera is writing, but in retrospect I think better to try to find some valid data rather than leave this blank.

Created an MSIX and installed it on my computer. This was largely a test but since it was successful I will probably try to use it and may try to use Github actions to build it automatically.

Included the bracket code help in the post help.

5/13/2020

Small fixes to the Image created from the File pdf extraction.

Quick updates to add pure to the remaining reports.

Photo List is now limited to most recent initially - the list is still smooth and responsive with all content loaded so I don't yet see the need to have search and basically limit getting to show all - but the load on show all is painfully slow as an initial load when you 'requested the program start' not 'requested all photos load'.

Implemented a very simple history report in html for all content types - originally I assumed that I would implement this as a wpf window but the since currently the 'real' idea here is to show history and let you manually merge what you need (ie - whole body got erased, no problem - just copy and paste it back from history) the html seems like it gets more functionality more quickly.

Found a bug in the Photo processing for my date patterns at the start of names - fixed. I think this is the last change here - I do wonder if this pattern should be a setting, it is pretty personal and will now catch more patterns...

Extended deletes in the photo list to allow multiples - so far I haven't needed this in other types so leaving those as single delete only for now.

Bumped up max photo load number to 100.

Most recent file is moved to the top of the recent list.

5/9/2020

Finished out a first implementation of the year and month navigation in the camera roll - I believe the structure will work but will need to work thru the styling a bit more to know.

Finished a Help Block that lists software used building this software.

Added PureCSS to the project as a static copy to inline into the photo metadata reports (and/or future reports) for quick and simple styling - didn't link it because I want full offline functionality with these local html files.

Originally in the metadata reports I opened the photo metadata report in an HTML view window but either the state of WebView, or the state of my use of it, means that find and context menu are not coming up?!? Switched that report to write to the temp folder and to open thru process start.

Added Camera Roll to the parts generation.

Added an about tab to the main app.

Bug fixes in new XMP Lens data extraction.

5/8/2020

Had trouble with a large panorama 1910 Sea of Sand - about 33k wide - throwing an exception in the ImageSharp based resizing code. From the GitHub issues it seemed like this shouldn't happen but it was slightly difficult to follow the technical library/image processing discussion plus be sure I understood what was in the currently packages - so asked in the gitter channel and 'an overflow in JpegFrame in SamplesPerLine' was identified. For now I just used a 20k wide export for the file - but hope to change this out later.

When working on a new post I found a puzzling truncation of long Titles extracted from Metadata - I reported https://github.com/drewnoakes/metadata-extractor/issues/474 but I am not sure if this is a questionable use of the metadata format by Adobe or a real issue - so at least for now I worked around by using the BitmapSource metadata which does extract the correct title - I haven't tested but my belief is that the BitmapSource way of doing it is going to be slower and more memory intensive, however extracting the correct title is important enough that any hit is worth it for now.

I had been getting photo titles from the IPTC metadata - this had worked well but I had a problem with some recent files with long titles - the titles were truncated... Initially I thought this was an issue with the metadata extraction since the Bitmapdata from a framework Bitmapsource and Exiftool both showed the Title without issue - but a very helpful comment online - https://github.com/drewnoakes/metadata-extractor/issues/474#issuecomment-626014133 - helped me quickly realize that I had not looked up and did not know this was to Adobe spec and also didn't realize that the generic GetMetadata call into MetadataExtractor didn't return XMP data. The XMP part is critical because that is where the full title is. Added the XMP Title and also some fallback lens info from XMP.

5/7/2020

Unexpected Errors today - I believe these are mostly .NET 3.1 Preview related and some may have to do with errors being exposed that I previously had hidden:
 - Null image source string - I don't believe this should normally error but it did - add the ImageCache converter into Photos (the only spot not using it) and it fixed the problem - also added in a Null Image converter based on https://stackoverflow.com/questions/5399601/imagesourceconverter-error-for-source-null to Utility - not currently using but the behavior in the other converter may not always be ideal
 - BitmapSource not initialized - previously just return a new instance worked - but now it is complaining that it is not initialized, added a small bitmap rather than new bitmap
 - Seeing the Aero2 error - you can continue past this so no big deal, but interesting this is coming up
 - No project Identity and missing Sqlite files - I am trying to stick at preview 3.1 but have had trouble several times with preview EF Core so moved back to stable and everything worked - I suspect some kind of version issue or compatibility with Sqlite
 - AngleSharp is throwing an encoding error - it can be bypassed but seems new with 3.1

The photo auto-save has been great for productivity but it has performance issues if you select too many files at once - added a switch so that at more than 5 files it processes them 1 at a time - worked with large first test batch.

Converted the window screen shot into a user control - to 'fully work' it looks dynamically for the StatusControlContext - this is an internal assumption but a good one for now especially for this 'non core' feature and as a nice way to integrate with low effort.

5/6/2020

Added an initial Screen Shot implementation - originally I had thought to use https://github.com/microsoft/Windows.UI.Composition-Win32-Samples/tree/master/dotnet/WPF/ScreenCapture but after looking more at the sample can Screen Capture docs I didn't understand how to quickly get a single screen shot of just the app window with minimal code - so went back to a traditional Visual render in WPF and rather than figure out that code again went to https://stackoverflow.com/questions/5124825/generating-a-screenshot-of-a-wpf-window. Still need to explore how to get it into each window with minimal hassle (a user control might work - probably can walk the visual tree up to window?).

Did some help text for the common fields - needs reformatting I think but basic idea works - test in Posts atm.

In writing the help realized that the title of a photo is not actually displayed on the photo page - still happy with the current layout that doesn't put it bold or at the top - but the title should be present, so made an overload of the caption to include it. Interestingly for general purpose use I ditched title in caption earlier - but for this specific case I think it makes sense.

A few CSS tweaks for PointlessWaymarks.com

5/5/2020

Tag Exclusions editor v1 done. Updated Tag generation done.

Used a combination of guard statement based on whether the db already exists and schema 'if' in the migration to maybe get a system where I can use the migrations and create new sites with a completely up to date .EnsureCreated db??? Might get too confusing? Or maybe just not worth it?? I think I will run it for now and see what happens.

5/4/2020

With Tags in place I noticed that there were a few tags in photos where I was fine with the tag appearing on the photo page (I think...) but the tag was more personal and I didn't like it showing in the tag list for search. This is easily solved and makes sense to me to have a tags exclusion table in the database. But adding a table would break the apps for previous db version - so time to setup migrations. I initially tried doing this via the EF Core migrations - but quickly ran into trouble since I don't have a single db to point it to - I could change that of course with a always present test db or... but it made me feel like this wasn't quite the scenario they were focused on - so I quickly found FluentMigrator and within a few minutes had it at least seemingly working with the table added!

First version of the Tags Exclusion Editor added - still needs works but basics in place.

Still excited about Fluent Migrations and it working as expected and quickly but now I wondering if I can build a new database with EnsureCreated and the get FluentMigrations to do the right thing... More research needed.

5/3/2020

Initial generation of Tags - an all page with Search and individual pages. Doing this triggered a refactoring in the Search Lists so that any content can potentially be generated (code has to be added for any new types but now not locked to IContentCommon).

5/2/2020

Put change indication in for Notes, Links and Content Type and filled in missing label Targets in those. I believe for now that this takes care of the Label Target and Change Indicator todos.

Added to Todos to look at https://github.com/microsoft/Windows.UI.Composition-Win32-Samples/tree/master/dotnet/WPF/ScreenCapture - this sample is a little to complex for a quick cut and paste but I suspect that the helpers and other information will be overall useful in addition to adding Screen Capture.

5/1/2020

I had moved over to iis for hosting for local dev and had written about it being easier - that was true for some initial work I did but I have now moved back to dotnet serve - in retrospect just as easy I think. Details that helped me -
   - dotnet dev-certs https --help - I didn't know this existed and found it via a google search on https://www.hanselman.com/blog/DevelopingLocallyWithASPNETCoreUnderHTTPSSSLAndSelfSignedCerts.aspx - seems like in many cases the dev certs should just work but I don't think anyone would argue that dev setups have odd hiccups over time esp. if you are constantly installing previews...
   - In dotnet serve you can't forget either -S or -p 443 in conjunction with a Hosts file entry such as 127.0.0.1 pointlesswaymarks.com
   - Probably worth noting ipconfig /flushdns - not sure what situations trigger needing this in which browsers if/when... but worth noting

I now think the problems I had with dotnet serve were actually about some misses doing 'everthing' correctly esp. moving from local debug in http vs https - everything above is documented and simple, but just enough details that I think when new to it I was confused occasionally by not getting everything correct all at once...

Changed the footer to use the core links like in the header and changed the format.

Added new checkboxes and javascript to the All Content List to filter by 'type' - here some content types are grouped together.

4/30/2020

Added a standard header tag - rather than add a responsive hamburger menu just went with a very simple set of text links. If I try migrating HikeLemmon a stronger menu component will be worth looking at but for now playing with simple coding on some menu option I actually wasn't sure anything was a 'win' vs just picking the most important links and showing them. Header also added to a number of additional pages.

Fixed a bug in related content where no related was still showing the related: heading with nothing following.

Added a content type to the content lists data - this is a setup for a future search setup on the 'all' list that allows filtering by type - I think that change will really improve the search.

Did a quick experiment with the TextBox wondering if I replaced the ContextMenu with my own if the Spell Checker would then grab that and use it - it didn't... it is likely that the problem can be solved either in code but I think benefit to time here is to just forget about this for now and let the return stripping in selected stay as a button for now.

4/29/2020

Body Content and Update Notes get visual updates and simple change indication.

Camera Roll updated with links to the Daily Galleries.

Added Camera Roll to the footer.

Fixed an error with new content and the CreatedUpdated control detecting them as not new both in the autofill of updated and in validation.

Worked on more automation for the photo imports.

4/28/2020

Added very basic Bracket code generation helpers and added link commands and buttons into the file editor.

File Content Help updated.

Fixed a bug in the related content generation that didn't include file content - this was obvious on the image pages where they are pdf image pages but weren't linking back to the file content.

Tried the 'Markdown Editor' Visual Studio extension for a week or so - loved the preview but couldn't seem to get spell checking to work so removed it - added Visual Studio Spellchecker in to cover the spell checking.

Published https://pointlesswaymarks.com/Files/PimaCounty/pima-county-multi-species-conservation-plan-2019-annual-report/pima-county-multi-species-conservation-plan-2019-annual-report.html to the main feed - first use of File Content on the index page and it seems to be working but I didn't see immediate rss updates which might need a check...

Updated Index to have the related content - will have to experiment on mobile to see if this works - I think on desktop it is not quite a pure/clean but is reasonable and great for content discovery.

Changed layout of the Camera Roll - after trying with a sort of list of list of days approach decided to go with a single list with date and info items mixed in with the photo items - maybe more 'date' items are needed (maybe years? maybe months?) but overall like this basic approach and also think this approach is a bit more interesting because it creates more difference between the daily galleries and the total list...

4/27/2020

More Help work - currently using the File Editor as a test. It may be worth circling back to see how localized text is commonly done in WPF and see if that is simple enough to try - I didn't go that direction immediately because my first idea was to keep everything 'in code'. Initial results are decent.

Body Content and Update Notes get GridSplitters between the content and preview - this is the most options for the least code so in that sense I love this solution - on my mind are saving and restoring the split position and possibly a shortcut to collapse/restore a side.

Fixed a typo bug in the index page generation when using FileContent in the Index Page.

Created and Updated get the value change info.

4/25/2020

Tags editor adds change detection.

All windows have closing with unsaved changes warnings but the ways tags are evaluated should be improved to be like the tags editor and closing an 'empty' editor is incorrectly flagged as 'has changes'.

Lost an edit by accidentally closing an editor while adding multiple files - as a result started work on detecting changes with the first experiments going into the FileEditorWindow and into the TitleSummarySlug control. Both are fairly simple at this point but in quick testing seem to work. I think the window work can be abstracted slightly to make this slightly faster/easier to get into all of the editors - having abstracted individual property change tracking before I suspect in this case it is simpler and less work to do it as 'one offs'.

The extract links was showing duplicates - eliminated. Also added an option to exclude specified links but not currently in use (left this code in but it didn't work out exactly like I thought - the original idea was to exclude PDFs that were the selected file of a FileContent, but since I often rename the incoming PDF this didn't actually work.

4/23/2020

Added the new from files to the Files interface - not too much code involved and while working on some Saguaro NPS documents found it was less confusing to be able to open all of them at once even if it wasn't any faster to edit.

In the early days of this project I started doing the Command setup in a Load method rather than in the constructor - this was partly to solve some problems I was having (I believe with the MVVMLight RelayCommand - but the earliest work here was copied over from the earliest prerelease .NET Core 3 versions that supported WPF so it could have been something else too) but also I thought it might be valuable to refresh the bindings as Load was called and thought requiring the Command properties to have notification changes like every other property would be nicely consistent - I think the experiment is over though and it didn't work out, needing property changed is just more friction for the commands and I think it this kind of vanilla WPF project it is natural to look for commands in the constructor so starting to walk this decision back - started with the main editors and lists.

Added a helper to get the SelectedText of a TextBox into the View Model - this was for the body content where pasting from PDF was bugging me because the text often copies with copious line breaks that reflect the original formatting nicely but are often imho not relevant for the web. Added a command to remove line breaks from selected text (for what it does the button is unusually prominent in the UI - this was the quickest solution - I added an idea to explore how to extend the textbox content menu and keep existing choices).

Refactored the PDF to Image generation I had added to easily get cover pages - this allowed this code to be included in the file editor (intuitive place for it while adding a new file...) and also allowed an extension to use pdftocairo to quickly make an image of any page number. This came with some complications in file naming but basically works nicely - the file naming complication is that pdftocairo adds a page number to your file name if you are not using 'singlefile' - I don't believe that new file name is included in the output and the exact format is either dependent on total pdf length and/or is not man-page-specified, worked around this but interesting that shelling out this process so quickly ran into issues with this approach, mostly it should not matter but could result in unexpected rare oddities...

4/22/2020

First version of the Camera Roll generation.

Fix a related content miss where photo pages weren't considering 'themselves' when getting daily photo content.

Move newer related content to all pages.

Fix typos in Main Window generation progress loops.

4/21/2020

In running validation on a page I found 2 issues:
 - Missing Doctype tag - fixed - for this content the browsers didn't care as far as I can tell but disappointed I missed this
 - Missing Sizes tag in the srcset images - the first images worked on for the site were all 'full width' (actually constrained but in the spirit of) and I had left out sizes - from reading this seems to be an 'error' but in past experimenting when you were just going to default to 100vw with nothing else in size it seemed that the browsers did the same thing with 100vw and nothing... But now there are the small thumbs and the photo gallery sizes in play - added sizes information. As in the past it seems like one challenge is that while sizes and srcset are incredibly powerful and don't require any javascript it also makes the images resistant to radical on the fly restyling - worth it for now anyway!

These two issues - but especially doctype - emphasize what a value a testing setup would be - perhaps this is possible now that switching setting files is possible? But the challenge is finding something that could be maintained and kept up to date - maybe just auto validation of all output?

Alpha sorted Tags in - I believe - all spots.

Daily Photos links now appearing as related content - Also reworked what appears here with the idea that if you bracket code linked content that it will also be included in the bottom related links which I think emphasizes the available content/resources in a nice way. This required additional bracket and other code to get this working especially in combination with subbing daily photo content for individual photo content.

4/20/2020

Added related content and previous/next to the Daily Photos page and did a little refactoring.

4/19/2020

Basic Daily Photo pages are now generating.

4/18/2020

Start work on the Photo Galleries - starting with daily...

4/14/2020

Improved the startup screen related code so that creating new sites happens much more nicely - I think the settings/settings file could be handled better in general but with this working I think more rework/refactoring waits.

Added the Build Date and GitInfo into the title bar - if this was a commercial project that would be a distraction, un-needed question and probably branding mistake - however because those aren't concerns if you have to ask someone or yourself about 'version' you can't get easier than in the title bar (haha so easier than having to ask is having everything logged and remotely available I suppose, and the version info is in the log but the log is local only so Title bar is still easier...)

Progress in the Initial Load.

Cleanup temporary files - I initially though I would do this via keeping track of each temporary file created - but after thinking about it for a few days decided instead to just look for files older than 2 days and delete those.

4/13/2020

In framework projects I have found the most 'versioning' utility for a desktop app in adding a build timestamp to the published Informational Version and showing that in the title bar. With the move to .NET Core I wanted to look again and try to see if there was something that might be better... I found two approaches to try:
 - https://www.meziantou.net/getting-the-date-of-build-of-a-dotnet-assembly-at-runtime.htm - the build datetime stamp I like so much is maybe a 'version' but maybe it is better as just a specific piece of metadata! This article shows how to quickly/easy add a buildtime attribute - love this, I think it would be more powerful to explore putting it into a Nuget package but for now just added the ?12 lines of code directly to projects.
 - https://github.com/kzu/GitInfo - Writes the Git info into the project on build - for this project where I am not yet doing/managing 'releases' if a friend used the project the best reference would probably be the git commit from the build - seems like this (including if there were uncommitted changes) plus buildtime gives a pretty good picture of what is going on.
 These approaches also leave the true Versions clean for use in either managing releases and/or automated CI style builds which I think is smart.

4/12/2020

Added a default icon and Github social image. Very simple but loads better than the default icon.

First version of a Settings file selector on startup - for now I am not going to support switching settings files on the fly because I am concerned there is too much room for error with scenarios like 'you have an editor window saving when you switch settings files - what happens???', but to get to work on my next project I need to support multiple sites which this does nicely. Only 'new' and 'existing' are supported but put in a placeholder for recent files which I think will be key functionality. Nearly zero testing at this point - this is a first minimal attempt.

4/11/2020

Added Extract Links to all relevant lists - this immediately proved to be very useful since there were links that I thought I had saved that I had not...

4/10/2020

Found an error where I was checking that Photo file names were unique but not slugs - when an image was imported with the same title but a different filename the same folder was used for both pictures - which is not the design and is not going to work in the current setup. Improved the slug checking and added it to other content types also.

Added a check that filenames are unique.

4/9/2020

Image and Photos can now start content from files and starting multiple files is supported - changes for this included option for an initial image in the editor and new code in the Action List.

Fixed a bug with the wrong year parsed from the 19xx photo name pattern - including this is a very personal choice but is such a productivity boost (and likely to not bother anyone else) that I think it is still work including.

Parse a Summary from photos from the title if the description is not present and add Exif description when present. While summary and title being nearly the same is not ideal the idea is to support/help quick photo content creation - basically provide as close to a complete piece of content from the image metadata import and don't have any barriers to providing more if the time and effort is there to do it.

Enhanced the autogeneration of the PDF Images so that an image editor launches with complete enough data to often be immediately saved.

Upgraded to .NET Core 3.1

Added a copy bracket code link to clipboard command to the Image Editor to help with the workflow of generating a cover preview.

Fixed several bugs in the Range Observable Collection with not passing in a concrete list (I am getting an error when that is done).

4/8/2020

Added Jot - https://github.com/anakic/Jot and WpfScreen - https://github.com/micdenny/WpfScreenHelper - and looked at a WPF Sample - https://github.com/microsoft/WPF-Samples/tree/master/Windows/SaveWindowState - and eventually drew heavy and nearly direct inspiration from Markdown Monster to set up tracking and restoring window position - there are so many edge cases that the thought this is perfect is completely laughable but I did try to look at several sources for information and inspiration to quickly put something together that I hope is quite good without unexpected errors and minimal edge cases. As part of this added a manifest file to hopefully ensure the Dpi Aware support is specified by the app - these were the better links that I found but I am not sure I actually understand the when/why/what very well here esp. of what happens by default in what versions and what needs to be setup (probably need to play around with settings esp. when connected to an external monitor to do some real world experimenting and testing...) https://docs.microsoft.com/en-us/windows/communitytoolkit/controls/wpf-winforms/webview, https://github.com/dotnet/wpf/issues/859, https://github.com/microsoft/WPF-Samples/blob/master/PerMonitorDPI/readme.md

Fixed a binding bug that was two way to a read only property in the Link List.

Fixed missing save button command binding in Settings.

Fixed several copy and paste bugs related to the background data updates.

Initial pdftocairo pdf preview generation added.

4/5/2020

Fixed a bug in the Content saves for images and photos where I hadn't quite gotten the recent adjustments to saving/generating right - core detail is that in early versions the files were always an over write operation (vs checking) which meant I didn't want to wait for that every time, but now the standard generate just checks for files rather than overwrites. Downside is if you switch out an image you have to force the regen...

Set up static weak events to serve as a way to alert/update the lists as content changes - light testing in the posts list and it was working nicely!

4/3/2020

Found while working on the Waterman Peak post that I was loading the content editor from the list item without refreshing from the database which had unintended consequences with the Waterman Peak post where I also added images, files and links so was bouncing between editors. Changed in all editors.

Even with the small current amount of content I was noticing that the list filter had some disappointing interaction where you would type and get a bad UI lag. Fixed this by removing the interaction trigger I was originally using in favor of triggering off the property change so that I could also take advantage of the Delay binding options which causes WPF to delay before updating the binding (this seems like insanity but works really well letting you type and the binding not be updated until you pause - which makes the UI appear to be nicely responsive.)

Did some working on setting up all labels with targets - Settings screen done.

First try work on unhandled exceptions - got handlers setup and basic logging in place - need to see which ones I could 'handle', which in this case I think means report and try to resume as an unhandled exception is as likely to be 'error handling bug' as fatal error in the spirit of 'out of memory'. *Changed this up a bit after looking at the Markdown Monster code - this is not the result of extensive testing but rather my impression of the rather solid state of Markdown Monster.

4/1/2020

Added a font size slider to the Body Editor (for me if you make the window full screen the system font size is too small) - also made the refresh preview button a little larger and added a small gap between it and the preview to make it easier to hit.

Added an open file button to the file list to open the document locally - very convenient.

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

A very helpful link of git sparse checkout on windows - glad I came across information on this fairly quickly - https://stackoverflow.com/questions/23289006/on-windows-git-error-sparse-checkout-leaves-no-entry-on-the-working-directory - basically the problem appears to be that if you use powershell (I suspect all versions but wonder about the latest open source cross platform versions) and follow variations of the most common sparse check out recipes the echo [your sparse checkout dir] >> .git/info/sparse-checkout is actually producing Unicode file with a BOM marker - instead you should do something like
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

The rendering with the WebView 'always on top' is in fact a known limitation of the control and I believe of XamlIslands in general - in the WPF/Winforms era a similar issue went by the informal name of 'airspace'. At the time one interesting solution basically involved rendering the control into a 'native to the GUi tech' image when you needed to do something on top of it - I don't think that I will explore that and have other UI binding ideas on work arounds... This issue is doesn't have any immediate resolution but at least seems to track this https://github.com/dotnet/runtime/pull/33060

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

I have made several tries at adding the newer MS WebView (soon to be WebView2) that uses an updated rendering engine with no success - Microsoft.Toolkit.Wpf.UI.Controls.WebView - in fact I have worked on sample projects and other experiments where I have also failed. In the early days I ran into code that would run but namespace issues that made everything seem broken. In recent attempts I was stopped because of various library issues including MVVMLight (EventToCommand) and the related change from Microsoft.Xaml.Behaviors.Wpf to System.Windows.Interactivity (the Microsoft.Xaml.Behaviors.Wpf is the open source version of System.Windows.Interactivity) and my use of RelayCommand. WebView is still not in use in the current version but for the first time it installs without issue - details:
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

Change in this version is to regenerate the JSON on each HTML generation.

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
