Traditional README information below - the tldr version might be taking a look at [Pointless Waymarks](https://PointlessWaymarks.com) and [cmiles - info](https://www.cmiles.info/) - these sites are managed and generated with the Pointless Waymarks CMS, photos are tagged via the Pointless Waymarks GeoTools, backups managed with Pointless Waymarks Cloud Backup and personal 'Memory' emails are generated with the MemoriesEmail task run via the Pointless Waymarks PowerShell Runner...

# Pointless Waymarks Project

*At this point there are no public installers/releases from the Pointless Waymarks Project - the code is MIT Licensed and public on GitHub to share with friends, colleagues and anyone who finds the code interesting or useful. This project is suitable for use if you enjoy debugging and working on code - scripts to create installers and update support are included in the apps!*

**The Pointless Waymarks Project is a set of applications that are connected by a focus on durable, rich, low maintenance, free, long-lasting, organized, content, stories, photos, location data and minutiae about the landscape, life, place, history and nature -- and by a love of programming, learning, craftsmanship and the joy of creating your own tools!**
  - [Windows Desktop CMS, Static Site Generator, Offline Site Viewer and supporting Command Line Tasks](https://github.com/cmiles/PointlessWaymarksProject) - described below. See [Pointless Waymarks](https://pointlesswaymarks.com/) and [cmiles - info](https://www.cmiles.info/), both sites are created with this system.
  - [Pointless Waymarks GeoTools](https://github.com/cmiles/PointlessWaymarksProject/blob/main/PointlessWaymarks.GeoToolsGui/README.md) - a Windows Desktop program to help with: geotagging Photos (both from gpx files and Garmin Connect), tagging geotagged photos with location information (state, park/monument name, etc) and downloading activities from Garmin Connect.
  - [Pointless Waymarks Cloud Backup](https://github.com/cmiles/PointlessWaymarksProject/blob/main/PointlessWaymarks.CloudBackupGui/README.md) - a data focused Windows backup program that uses Amazon S3, Cloudflare R2 or Wasabi. Features include: MD5 file comparisons, a maximum runtime for backups, Excel reports and easy scheduling via the Windows Task Scheduler. This program does not created versioned backups or have an mechanism to restore your files -> instead it focuses on creating a simple/direct backup of your files on S3/R2 - with your files on S3/R2 you can easily explore your backup on the web or via free clients on nearly any OS and in an emergency there is a very good chance you can find some reasonable way to see/view/access your backups.
  - [Pointless Waymarks Feed Reader](https://github.com/cmiles/PointlessWaymarksProject/blob/main/PointlessWaymarks.FeedReaderGui/README.MD) - a simple Windows Desktop Feed Reader. No folders to organize, no online account and no temptation to doom scroll on your phone - this is a Windows Desktop only program for reading content that you care about with very low setup and ceremony.
  - [Pointless Waymarks PowerShell Runner](https://github.com/cmiles/PointlessWaymarksProject/blob/main/PointlessWaymarks.PowershellRunnerGui/README.md) - a Windows Desktop program to run simple PowerShell scripts. The scripts can be scheduled or run on demand and the output from the runs is recorded (as encrypted text) in a SQLite database for easy review. For me this program replaces Windows Task Scheduler as a nicer way to run simple scheduled jobs including [Cloud Backups](https://github.com/cmiles/PointlessWaymarksProject/blob/main/PointlessWaymarks.CloudBackupGui/README.md), various [Sensor and Data Backups](https://github.com/cmiles/GMH-Backups/tree/main) and the Memories Email Task from this project.

Todo Lists, Idea Lists and a Development Log are [found in a DevNotes.md file that is versioned along with the code](/DevNotes.md).

If you have questions or comments please contact me at pointless@pointlesswaymarks.com.

## Pointless Waymarks CMS

Pointless Waymarks CMS is a .NET Core 8 Windows WPF GUI for generating a static website based on database information and a settings file. It is not an 'all purpose' solution - instead it focuses on:
 - Locally Generated Static Sites - low cost, low maintenance, high durability, easy backup, doesn't have to be on the web to be useful.
 - Database Driven CMS - flexibility in creating and updating content with the potential to generate multiple output formats.
 - Custom Software - optimized support for a limited set of content types and a specific set of workflows.
 - Local/Offline Windows Desktop WPF GUI Editor - no hosting to pay for, no server to maintain, no containers to orchestrate... WPF because it is my favorite Windows GUI technology atm and because the experimentation/learning/coding I do here flows back into my day job where I create and maintain a Retail Inventory Management and Reporting system that has a WPF Front-End.
 - Simple Functional Presentation with a Focus on Content - my experience is that simple presentations of interesting content can survive, have impact and be meaningful for many many years (indefinitely?) without heavy revisions, constant updates or conversion into the latest style/newest framework.

### Launch Screen

Options to launch recent projects or create a new project.

![Launch Screen](PointlessWaymarks.CmsScreenShots/LaunchScreen.jpg "Launch Screen")

### Content Lists

![All Content List](PointlessWaymarks.CmsScreenShots/AllContentList.jpg "All Content List")

The first tab in the application is an All Content list. Like all of the lists you can sort and filter to find content and use commands from the menu bar, context menu and quick action buttons to edit, update and create content. The default sorting puts the most recently updated/created items at the top which often means this list is all you need to work efficiently. All lists update automatically in the background to reflect the latest changes.

![List Search Builder](PointlessWaymarks.CmsScreenShots/ListSearchBuilder.jpg "List Search Builder")

Each Content Type has a dedicated list that allows access to content-specific commands not available in the All Content list. Content lists start by loading a limited number of recent entries - this allows even very large lists to load quickly with the content that you are most likely to be actively working on. Loading the full list is a single button click.

The ability to export to Excel, edit, and import the changes back into the Pointless Waymarks CMS is provided for all content types and most fields. Many updates are more easily made inside the program but Excel provides a powerful interface for complicated/bulk edits.

![Posts List](PointlessWaymarks.CmsScreenShots/PostsList.jpg "Posts List")

Photographs are a central content type and there is support for reading information from the photo's metadata. This often allows the painless import of large batches of photographs with minimal clean up afterwards.

The Photo List supports searching for field like focal length and iso in addition to standard search fields like titles and tag. Photo items have buttons to quickly find similar photos based on details such as Camera Make, Lens, Aperture, Shutter Speed, etc. The Reports menu allows you to quickly find potential problems such as 'No Tags' or 'Blank License' and has an option to export all of a photo's raw metadata to an html file.

![Photos List](PointlessWaymarks.CmsScreenShots/PhotoList.jpg "Photos List")

A list view of content that has spatial information alonside a map is also available.

![Content Map List](PointlessWaymarks.CmsScreenShots/PhotoList.jpg "Content Map List")

### Content Editors

The content editors are intended to be simple, helpful and functional. Change and validation indicators, previews and help with common editing actions are provided. Spatial types - Points, Lines and GeoJson have [Leaflet](https://leafletjs.com/) based previews.

![Post Content Editor](PointlessWaymarks.CmsScreenShots/PostEditorWithPreview.jpg "Post Content Editor")
![Photo Content Editor](PointlessWaymarks.CmsScreenShots/PhotoEditor-ShowingFileAndPhotoMetadata.jpg "Photo Content Editor")
![Point Content Editor](PointlessWaymarks.CmsScreenShots/PointEditor.jpg "Point Content Editor")
![Line Content Editor](PointlessWaymarks.CmsScreenShots/LineEditorMapTab.jpg "Line Content Editor")

### Tags

Tags are a primary way of connecting and organizing content - a Tag Editor, along with Excel Export/Import, provide support for organizing/correcting/updating tags.

This software has no support for storing completely private content - but it does have support for excluding Tags from the site's various search pages and indicating to search engines not to index excluded Tag Pages. This can be a good way of providing some 'modesty' for tags that you might not want to delete - but that you also don't want to be prominent.

![Tag List](PointlessWaymarks.CmsScreenShots/TagList.jpg "Tag List")

### Menu Links

Content is the focus of this project and admin/widget/header/menu options are intentionally minimal. One of the few options is to use the Menu Links editor to create a very simple menu for the site.

![Menu Links](PointlessWaymarks.CmsScreenShots/MenuLinksEditor.jpg "Menu Links Editor")

### File Change Tracking

Deploying the site is simply a matter of syncing the content from the generated local site to its destination. To help with that process the program tracks written files and has options to export a list of files, create very basic S3 cli scripts or use the built in S3 uploader. There is also support to detect changed and no longer needed files on S3.

![Written Files List](PointlessWaymarks.CmsScreenShots/WrittenFilesList.jpg "Written Files List")
![S3 Uploader](PointlessWaymarks.CmsScreenShots/S3Uploader.jpg "S3 Uploader")

### Feature Intersection Tagging for types with Spatial Data

With some setup involving downloading/creating GeoJson files and putting together a settings file you can tag lines, points and photos with values from GeoJson reference data. This is done by checking for intersections between your downloaded/created reference data and the spatial data from your content - you can specify what property is used to create a Tag.

This certainly doesn't replace tagging 'by hand', but having details like National Forests, National Parks, State Line, National Monuments, State, County, etc. consistently and automatically tagged can be an advantage in organizing your content. In the screen shot below all of the tags were generated by the Feature Intersection Tags feature and data including [PAD-US from the U.S. Geological Survey](https://www.usgs.gov/programs/gap-analysis-project/science/pad-us-data-overview).

See [the Feature Intersection Tags documentation](PointlessWaymarks.FeatureIntersectionTags/README.md) for details.

![Feature Site Intersection Tags](PointlessWaymarks.CmsScreenShots/FeatureIntersectionsTagsExample.jpg "Feature Site Intersection Tags")

## (Local, Offline) Site Viewer

A viewer for the on-disk version of the site is available both in the editor and as a stand alone program. This makes it possible to browse your local site without configuring a local web server or publishing your changes. Links to the site are opened in the viewer - external links are opened in your default browser.

One important reason that a local viewer is included is that not all content needs to be online!

![Local Site Viewer](PointlessWaymarks.SiteViewerScreenShots/LocalSiteViewer.jpg "Local Site Viewer")

## Tasks

Part of the project are a number of 'Tasks' - these are Console Applications that provide extra functionality and are intended to be run thru the Windows Task Scheduler.

### 'Memories' Email

The 'PointlessWaymarks.Task.MemoriesEmail' console app can generate an email with links to items created in previous years on the site. The app is driven by a settings file where you can setup the years back, email settings and what site to get information from. This program can be setup in the Windows Task Scheduler to run daily for fun/interesting emails about past content! [More Information](PointlessWaymarks.Task.MemoriesEmail/README.md)

![Memories Email](PointlessWaymarks.CmsScreenShots/MemoriesEmail.jpg "Memories Email")

### Photo Pickup

The 'PointlessWaymarks.Task.PhotoPickup' console app is designed to pickup photographs from a local folder and add them to a site. Setup to run daily in the Windows Task Scheduler this can be an easy way to process a batch of photographs with having to even open the CMS program and combined with a sync program like Dropbox this can be an easy way to add photographs from a mobile device. [More Information](PointlessWaymarks.Task.PhotoPickup/README.md)

### Garmin Connect GPX Import

The 'PointlessWaymarks.Task.GarminConnectGpxImport' console app can download Activitites with location information from Garmin Connect and, optionally, import them into a Pointless Waymarks CMS Site as Line Content. In no way is any part of the Pointless Waymarks Project desgined as a replacement for any part of Garmin Connect - but if you care about the landscape, your history and adventures it is likely worth archiving your Garmin Connect data locally so that you have/own it no matter what happens with Garmin Connect and your Garmin Connect account. This is also an easy way to create Line Content in a Pointless Waymarks CMS site. [More Information](PointlessWaymarks.Task.GarminConnectGpxImport/README.md)

### Publish Site to S3

The 'PointlessWaymarks.Task.PublishSiteToS3' console app detects changes, generates the site and publishes the site to S3 (Amazon S3, Cloudflare R2, Wasabi). [More Information](PointlessWaymarks.Task.PublishSiteToS3/README.md)

## InnoSetup Based Installers and Program Update Notifications

![Program Update Notice over the All Items List](PointlessWaymarks.CmsScreenShots/MainListWithProgramUpdateNotice.jpg "Program Update Notice over the All Items List")

PowerShell Scripts are included that can generate installers for the programs using [Inno Setup](https://jrsoftware.org/isinfo.php). These scripts create the installers with names and inforamtion that the programs are aware of. If you setup the programs to know where the installers are located the programs will check for updates on startup and offer to close the program and start the install. Keeping with the offline first theme of this program - and because there are no current plans for official install packages - this system is designed primarily to work either on your computer or from a file share on your network. These scripts may need some modification for your local environment.

## Password Protected Sites via Cloudflare Workers

Not currently incorporated into the program in any way, but included in this repo, is a simple Cloudflare Worker script for Basic Auth (I have used this successfully for over a year but Cloudflare Workers aren't my current passion so use with caution...). This provides a very simple zero cost (Cloudflare Workers are available on their free plan) way to password protect a site. There is no sense of 'user accounts' or options to change/recover passwords so this is only relevant in limited scenarios, but I have found this to be a nice way to put content online for myself, friends and family without making it public.

## Elements of this Software that might be Reused

Part of the reason that this code is made public and shared with an MIT License is so that you can easily reuse pieces of the software - open source code has made it possible to build this software and I hope that this software provides value to other developers! Hopefully there are many interesting details in this software - but I think there are a few details worth calling out as potentially re-usable.

**ExcelInteropExtensions** - A very useful approach to getting user data from Excel is reading directly from the Excel application. This can be messy (give the user any message/help you want but you will still spend time explaining that reading from Excel isn't working because they have a cell open for editing...) - but especially for power users it can avoid confusion over what data is saved vs on screen and can reduce repetitive steps like saving/picking/dragging/etc. This is not my first .NET journey into Excel interop code and if you are exploring this approach I encourage you to look at and/or reuse this code. It is very heavily based on [Automate multiple Excel instances - CodeProject](https://www.codeproject.com/articles/1157395/automate-multiple-excel-instances) by James Faix. Faix's code, and other code in this vein, all ultimately link back to [Andrew Whitechapel : Getting the Application Object in a Shimmed Automation Add-in (archived link)](https://web.archive.org/web/20130518152056/http://blogs.officezealot.com/whitechapel/archive/2005/04/10/4514.aspx) (the source I used in the mid-2000s when I first started to do .NET Excel Interop!).

**WPFCommon** - Nothing here is revolutionary and you should certainly first consider using some of the larger well-supported WPF MVVM libraries as the basis for your app! But I believe the code noted below has value especially for small to medium WPF applications built by single devs or small teams where there is a heavy emphasis functionality over styling. There are a number of pieces in the project but worth considering are:
  - ThreadSwitcher - based on Raymond Chen's [C++/WinRT envy: Bringing thread switching tasks to C# (WPF and WinForms edition)](https://devblogs.microsoft.com/oldnewthing/20190329-00/?p=102373) -  this adds the ability to write 'await ThreadSwitcher.ResumeForegroundAsync();' and 'await ThreadSwitcher.ResumeBackgroundAsync();' to get onto/off of the UI Thread. While MVVM/Binding can help you write code that doesn't have to be aware of which thread you are on I have found it impractical to produce a user friendly/focused WPF GUI without occasionally need to interact with the main UI thread... If you have code where you need to control running on/off the main UI thread ThreadSwitcher is a pleasant and productive pattern in GUI code.
  - StatusControl - Over time I've found that with Desktop Apps I want to have an easy, generic, way to run tasks in the background, block the UI, show cancellation buttons, stream text progress information, display messages and show in-app toast. The StatusControl in this project has a Context and Control that can be added to a control/page/window to handle these scenarios. This is a compromise - this single control won't help you produce an infinite variety of perfect intricate UI interactions, but it does provide a good-enough solution that can be applied quickly and easily to run run tasks in the background and show progress.
  - WindowScreenShot - It turns out that it is quite nice to get screen shots of your app's window but also quite tricky to get this to happen correctly in all situations... The ScreenShot control in this project is based on code from [Capturing screenshots using C# and p/invoke](https://www.cyotek.com/blog/capturing-screenshots-using-csharp-and-p-invoke).

## Tools and Libraries

I am incredibly grateful to the all the people and projects that make it possible to rapidly build useful, open, low/no-cost software. Below is a mostly-up-to-date-and-largely-comprehensive list of tools/packages/libraries/etc. that are used to build the this project:

**Tools:**
 - [Visual Studio IDE](https://visualstudio.microsoft.com/), [.NET Core (Linux, macOS, and Windows)](https://dotnet.microsoft.com/download/dotnet-core)
 - [ReSharper: The Visual Studio Extension for .NET Developers by JetBrains](https://www.jetbrains.com/resharper/)
 - [GitHub Copilot · Your AI pair programmer · GitHub](https://github.com/features/copilot)
 - [Metalama: A Framework for Clean & Concise Code in C#](https://www.postsharp.net/metalama)
 - [PowerShell](https://github.com/PowerShell/PowerShell)
 - [AutoHotkey](https://www.autohotkey.com/)
 - [Beyond Compare](https://www.scootersoftware.com/)
 - [Compact-Log-Format-Viewer: A cross platform tool to read & query JSON aka CLEF log files created by Serilog](https://github.com/warrenbuckley/Compact-Log-Format-Viewer)
 - [DB Browser for SQLite](https://sqlitebrowser.org/)
 - [ExifTool by Phil Harvey](https://exiftool.org/) and [Oliver Betz | ExifTool Windows installer and portable package](https://oliverbetz.de/pages/Artikel/ExifTool-for-Windows)
 - [Fork - a fast and friendly git client for Mac and Windows](https://git-fork.com/)
 - [grepWin: A powerful and fast search tool using regular expressions](https://github.com/stefankueng/grepWin)
 - [Inno Setup](https://jrsoftware.org/isinfo.php)
 - [LINQPad - The .NET Programmer's Playground](https://www.linqpad.net/)
 - [Greenfish Icon Editor Pro](http://greenfishsoftware.org/gfie.php)
 - [Notepad++](https://notepad-plus-plus.org/)
 - [RegexBuddy: Learn, Create, Understand, Test, Use and Save Regular Expression](https://www.regexbuddy.com/)

**Core Technologies:**
 - [dotnet/core: Home repository for .NET Core](https://github.com/dotnet/core)
 - [dotnet/wpf: WPF is a .NET Core UI framework for building Windows desktop applications.](https://github.com/dotnet/wpf). MIT License.

**Data:**
 - [dotnet/efcore: EF Core is a modern object-database mapper for .NET. It supports LINQ queries, change tracking, updates, and schema migrations.](https://github.com/dotnet/efcore)
 - [fluentmigrator/fluentmigrator: Fluent migrations framework for .NET](https://github.com/fluentmigrator/fluentmigrator) -  documentation](https://fluentmigrator.github.io/)
 - [SQLite](https://www.sqlite.org/index.html) - An absolutely brilliant project - having a Public Domain option for such a high quality data store that can be used locally and cross platform is amazing! Public Domain.

**Images:**
 - [GitHub - miromannino/Justified-Gallery: Javascript library to help creating high quality justified galleries of images.](https://github.com/miromannino/Justified-Gallery). MIT License.
 - [drewnoakes/metadata-extractor-dotnet: Extracts Exif, IPTC, XMP, ICC and other metadata from image, video and audio files](https://github.com/drewnoakes/metadata-extractor-dotnet) - Used to read the metadata in Photographs - there are a number of ways to get this data but it is amazing to have a single go to library to work with that already handles a number of the (many, many, many...) issues. Apache License, Version 2.0.
 - [Pictogrammers - Open-source iconography for designers and developers](https://pictogrammers.com/)
 - [saucecontrol/PhotoSauce: MagicScaler high-performance, high-quality image processing pipeline for .NET](https://github.com/saucecontrol/PhotoSauce) - Fast high quality Image Resizing. If you personally care about image quality image resizing becomes a complicated topic very quickly and I think the results from this library are excellent. Ms-Pl.
 - [GitHub - dimsemenov/PhotoSwipe: JavaScript image gallery for mobile and desktop, modular, framework independent](https://github.com/dimsemenov/PhotoSwipe) - [PhotoSwipe Examples and Documentation](https://photoswipe.com/getting-started/). MIT License.
 - [Raleway - Google Fonts](https://fonts.google.com/specimen/Raleway/about)
 - [ElinamLLC/SharpVectors: SharpVectors - SVG# Reloaded: SVG DOM and Rendering in C# for the .Net.](https://github.com/ElinamLLC/SharpVectors) - support for using SVG in WPF applications including Markup Extensions and Controls. BSD 3-Clause License.
 - [mono/taglib-sharp: Library for reading and writing metadata in media files](https://github.com/mono/taglib-sharp) - for reading tags this application uses other libraries - but TagLib# is notable for also writing metadata. LGPL-2.1 license.
 - [drewnoakes/xmp-core-dotnet: .NET library for working with the Extensible Metadata Platform (XMP)](https://github.com/drewnoakes/xmp-core-dotnet/) - The goto C# library if you want to read/write XMP files.

**Excel:**
 - [Automate multiple Excel instances - CodeProject](https://www.codeproject.com/Articles/1157395/Automate-multiple-Excel-instances) - James Faix's excellent code for getting references to running Excel instances was pulled into this project, converted for style and upgraded to .NET Core. The basic approach in this article comes from a 2005 post by Andrew Whitechapel titled 'Getting the Application Object in a Shimmed Automation Add-in' - http://blogs.officezealot.com/whitechapel/archive/2005/04/10/4514.aspx. The post by Andrew Whitechapel is now only available thru the Wayback Machine - [Andrew Whitechapel : Getting the Application Object in a Shimmed Automation Add-in](https://web.archive.org/web/20130518152056/http://blogs.officezealot.com/whitechapel/archive/2005/04/10/4514.aspx).
 - [ClosedXML](https://github.com/ClosedXML/ClosedXML) - A great way to read and write Excel Files - I have years of experience with this library and it is both excellent and well maintained. MIT License.

**Maps/GIS:**
 - [sealbro/dotnet.garmin.connect: Unofficial garmin connect client](https://github.com/sealbro/dotnet.garmin.connect). MIT License.
 - [GeoNames Web Service](https://www.geonames.org/export/web-services.html) - [GeoNames](https://www.geonames.org/) is a great resource for place name lookup and offers both offline information downloads and a Web API which can be used for limited use for free.
 - [mattjohnsonpint/GeoTimeZone: Provides an IANA time zone identifier from latitude and longitude coordinates.](https://github.com/mattjohnsonpint/GeoTimeZone) - Great in combination with spatial data for determining times (offline!). MIT License.
 - [Leaflet - a JavaScript library for interactive maps](https://leafletjs.com/) - [On GitHub](https://github.com/Leaflet/Leaflet). BSD-2-Clause License.
   - [elmarquis/Leaflet.GestureHandling: Brings the basic functionality of Google Maps Gesture Handling into Leaflet. Prevents users from getting trapped on the map when scrolling a long page.](https://github.com/elmarquis/Leaflet.GestureHandling). MIT License.
   - [domoritz/leaflet-locatecontrol: A leaflet control to geolocate the user.](https://github.com/domoritz/leaflet-locatecontrol). MIT License.
   - [GitHub - koddas/Leaflet.awesome-svg-markers: Colorful, iconic & retina-proof SVG markers for Leaflet, based on Leaflet.awesome-markers](https://github.com/koddas/Leaflet.awesome-svg-markers/tree/master). MIT License.
 - [NetTopologySuite/NetTopologySuite: A .NET GIS solution](https://github.com/NetTopologySuite/NetTopologySuite). [NetTopologySuite License](https://github.com/NetTopologySuite/NetTopologySuite/blob/develop/License.md) - Nuget Package listed as BSD-3-Clause.
 - [NetTopologySuite/NetTopologySuite.IO.GPX: GPX I/O for NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite.IO.GPX). BSD-3-Clause License.
 - [NetTopologySuite/NetTopologySuite.IO.GeoJSON: GeoJSON IO module for NTS.](https://github.com/NetTopologySuite/NetTopologySuite.IO.GeoJSON). BSD-3-Clause License.
 - [Open Topo Data](https://www.opentopodata.org/) - Provides an open and free Elevation API and offers both a public service and the code to host the service yourself (including scripts/information to get the needed source data) - [GitHub: ajnisbet/opentopodata: Open alternative to the Google Elevation API!](https://github.com/ajnisbet/opentopodata). (Code) MIT License.

**Wpf:**
 - [punker76/gong-wpf-dragdrop: The GongSolutions.WPF.DragDrop library is a drag'n'drop framework for WPF](https://github.com/punker76/gong-wpf-dragdrop). BSD-3-Clause License.
 - [anakic/Jot: Jot is a library for persisting and applying .NET application state.](https://github.com/anakic/Jot) - Used to save application state most prominently main window position.
 - [hexinnovation/MathConverter: An All-in-One XAML Converter](https://github.com/hexinnovation/MathConverter) - A welcome addition to binding options in WPF that allows specifying math operations as a ConverterParameter to modify bindings without writing (another) custom converter.
 - [jamesmontemagno/mvvm-helpers: Collection of MVVM helper classes for any application](https://github.com/jamesmontemagno/mvvm-helpers) - Code for Commands from this project was brought into the Pointless Waymarks code. MIT License.
 - [Dirkster99/NumericUpDownLib: Implements numeric up down WPF controls](https://github.com/Dirkster99/NumericUpDownLib) - These up/down controls are missing from WPF - nice to find an updated open source library that provides these! MIT License.
 - [ookii-dialogs/ookii-dialogs-wpf: Awesome dialogs for Windows Desktop applications built with Microsoft .NET (WPF)](https://github.com/ookii-dialogs/ookii-dialogs-wpf) - easy access to several nice dialogs. [License of Ookii.Dialogs.Wpf.NETCore 2.1.0](https://www.nuget.org/packages/Ookii.Dialogs.Wpf.NETCore/2.1.0/License).
 - [SimpleScreenShotCapture](https://github.com/cyotek/SimpleScreenshotCapture) and [Capturing screenshots using C# and p/invoke](https://www.cyotek.com/blog/capturing-screenshots-using-csharp-and-p-invoke)- An example project and blog post with information on and code for capturing screen and window shots using native methods. Used this as the basis for a WPF/[WpfScreenHelper](https://github.com/micdenny/WpfScreenHelper) version - the advantage over traditional WPF based window image methods is that XamlIsland type controls can be captured. Creative Commons Attribution 4.0 International License.
 - [TinyIpc](https://github.com/steamcore/TinyIpc) - Windows Desktop Inter-process Communication wrapped up into a super simple to use interface for C#. After trying a number of things over the years I think this technology wrapped into a great C# library is absolutely a key piece of .NET Windows desktop development that provides a reasonable way for your apps to communicate with each other 'locally'. MIT License.
 - [WindowsCommunityToolkit](https://github.com/CommunityToolkit/WindowsCommunityToolkit) - [Microsoft.Toolkit.Mvvm 7.1.2](https://www.nuget.org/packages/Microsoft.Toolkit.Mvvm/) - The Mvvm Toolkit provides a number of good tools including SourceGenerators that can implement IPropertyNotificationChanged! MIT License.
 - [micdenny/WpfScreenHelper: Porting of Windows Forms Screen helper for Windows Presentation Foundation (WPF). It avoids dependencies on Windows Forms libraries when developing in WPF.](https://github.com/micdenny/WpfScreenHelper) - help with some details of keeping windows in visible screen space without referencing WinForms. MIT License.
 - [dotnet/DataGridExtensions: Modular extensions for the WPF DataGrid control](https://github.com/dotnet/DataGridExtensions) - Easy way to add simple filtering and other features especially to an existing DataGrid. MIT License.
 - [dfinke/PowerShellConsole: Create a PowerShell Console using the AvalonEdit control](https://github.com/dfinke/PowerShellConsole/tree/master) - Used code from this project to create an AvalonEdit based PowerShell control for the PowerShell Script Runner Project. Apache License 2.0.
 - [icsharpcode/AvalonEdit: The WPF-based text editor component used in SharpDevelop](https://github.com/icsharpcode/AvalonEdit) - A long lived, powerful, text editor for WPF. MIT license.

**Html:**
 - [AngleSharp - Home](https://anglesharp.github.io/) - [AngleSharp/AngleSharp: The ultimate angle brackets parser library parsing HTML5, MathML, SVG and CSS to construct a DOM based on the official W3C specifications.](https://github.com/AngleSharp/AngleSharp) - Mainly used for parsing web pages when creating links. MIT License.
 - [chartjs/Chart.js: Simple HTML5 Charts using the canvas tag](https://github.com/chartjs/Chart.js)
 - [arminreiter/FeedReader: C# RSS and ATOM Feed reader library. Supports RSS 0.91, 0.92, 1.0, 2.0 and ATOM. Tested with multiple languages and feeds.](https://github.com/arminreiter/FeedReader)
 - [Flurl](https://flurl.dev/) - [GitHub - tmenier/Flurl: Fluent URL builder and testable HTTP client for .NET](https://github.com/tmenier/Flurl). MIT License.
 - [zzzprojects/html-agility-pack: Html Agility Pack (HAP) is a free and open-source HTML parser written in C# to read/write DOM and supports plain XPATH or XSLT. It is a .NET code library that allows you to parse "out of the web" HTML files.](https://github.com/zzzprojects/html-agility-pack) - Used in the Memories email program to parse html. MIT License.
 - [shps951023/HtmlTableHelper: Mini C# IEnumerable object to HTML Table String Library](https://github.com/shps951023/HtmlTableHelper) - used for quick reporting output like the Photo Metadata Dump. MIT License.
 - [HtmlTags/htmltags: Simple object model for generating HTML](https://github.com/HtmlTags/htmltags) - Currently this project uses a combination of T4 templates and tags built by this library to produce HTML. Apache License, Version 2.0.
 - [lunet-io/markdig: A fast, powerful, CommonMark compliant, extensible Markdown processor for .NET](https://github.com/lunet-io/markdig) and [Kryptos-FR/markdig.wpf: A WPF library for lunet-io/markdig https://github.com/lunet-io/markdig](https://github.com/Kryptos-FR/markdig.wpf) - Used to process Commonmark Markdown both inside the application and for HTML generation. BSD 2-Clause Simplified License and MIT License.
 - [SebastianStehle/mjml-net](https://github.com/SebastianStehle/mjml-net) - An unofficial port of [Mailjet Markup Language](https://mjml.io/) for .NET - this is a good way to ease the pain of building HTML for email. MIT License.
 - [Pure](https://purecss.io/) - Used in the reporting output for simple styling. GitHub: [pure-css/pure: A set of small, responsive CSS modules that you can use in every web project.](https://github.com/pure-css/pure/). BSD and MIT Licenses.
 - [sakura: a minimal classless css framework / theme](https://oxal.org/projects/sakura/) - Minimal Classless Css. GitHub: [oxalorg/sakura: a minimal css framework/theme.](https://github.com/oxalorg/sakura). MIT License.

**Data Transfer:**
 - [aws/aws-sdk-net: The official AWS SDK for .NET](https://github.com/aws/aws-sdk-net/) - For Amazon S3 file management. After years of using this library I appreciate that it is constantly updated! Apache License 2.0.
 - [shrayasr/pinboard.net](https://github.com/shrayasr/pinboard.net/blob/master/LICENSE) - Easy to use wrapper for [Pinboard - 'Social Bookmarking for Introverts'](http://pinboard.in/). MIT License.
 - [bcwood/PressSharper: A C# class library for parsing WordPress XML export data.](https://github.com/bcwood/PressSharper) - the code from PressSharper was pulled into this project, updated for .net5.0, and lightly refactored and reformatted. PressSharper was forked from forked from [dreadwail/press_sharp: A C# class library for parsing Wordpress XML export data.](https://github.com/dreadwail/press_sharp). MIT License.

**General:**
 - [replaysMike/AnyClone: A CSharp library that can deep clone any object using only reflection.](https://github.com/replaysMike/AnyClone). MIT License.
 - [commandlineparser/commandline: The best C# command line parser that brings standardized \*nix getopt style, for .NET. Includes F# support](https://github.com/commandlineparser/commandline) - MIT License.
 - [CompareNETObjects](https://github.com/sgray128/Compare-Net-Objects) - Comparison of object properties that stays quick/easy to use but has more options than you would be likely to create with custom reflection code - and potentially more durability than hand coded comparisons. Ms-PL License.
 - [zzzprojects/System.Linq.Dynamic.Core: The .NET Standard / .NET Core version from the System Linq Dynamic functionality.](https://github.com/zzzprojects/System.Linq.Dynamic.Core) - In addition to interesting dynamic query building via strings this library has a very easy collection of methods to build types at runtime. Apache-2.0 license.
 - [GitHub - danm-de/Fractions: A fraction data type to calculate with rational numbers.](https://github.com/danm-de/Fractions) - Used in the Shutter Speed Content List Search - this makes dealing with fractions quite easy! Copyright (c) 2013-2017, Daniel Mueller <daniel@danm.de>. All rights reserved. Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
     1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
     2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
     - THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ""AS IS"" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 - [fluentscheduler/FluentScheduler: Automated job scheduler with fluent interface for the .NET platform.](https://github.com/fluentscheduler/FluentScheduler/tree/version-5)   
 - [kzu/GitInfo: Git and SemVer Info from MSBuild, C# and VB](https://github.com/kzu/GitInfo) - Git version information. MIT License.
 - [IdentityModel/IdentityModel: .NET standard helper library for claims-based identity, OAuth 2.0 and OpenID Connect.](https://github.com/IdentityModel/IdentityModel) - Apache 2.0 License.
 - [rickyah/ini-parser: Read/Write an INI file the easy way!](https://github.com/rickyah/ini-parser) - the ease of working with json makes it quite attractive but it is not as easy to edit by hand as an .ini file imho. MIT License.
 - [Microsoft.Recognizers.Text provides recognition and resolution of numbers, units, and date/time expressed in multiple languages](https://github.com/microsoft/Recognizers-Text) - An impressive Date and Time parsing library - the output is detailed and uses generic string/object data types (rather than custom .NET types) so takes some work to parse, but the excellent results and ability to easily recognize when the user has only input a date, or only a time, or both, or a range... is powerful. MIT License.
 - [thomasgalliker/ObjectDumper: ObjectDumper is a utility which aims to serialize C# objects to string for debugging and logging purposes.](https://github.com/thomasgalliker/ObjectDumper) - A quick way to convert objects to human readable strings/formats. Apache License, Version 2.0.
 - [mcintyre321/OneOf: Easy to use F#-like \~discriminated\~ unions for C# with exhaustive compile time matching](https://github.com/mcintyre321/OneOf). MIT License.
 - [App-vNext/Polly: Polly is a .NET resilience and transient-fault-handling library that allows developers to express policies such as Retry, Circuit Breaker, Timeout, Bulkhead Isolation, and Fallback in a fluent and thread-safe manner. From version 6.0.1, Polly targets .NET Standard 1.1 and 2.0+.](https://github.com/App-vNext/Polly) - Great library for handling retry logic in .NET. New BSD License.
 - [serilog/serilog: Simple .NET logging with fully-structured events](https://github.com/serilog/serilog). Easy full featured logging. Apache-2.0 License.
   - [RehanSaeed/Serilog.Exceptions: Log exception details and custom properties that are not output in Exception.ToString().](https://github.com/RehanSaeed/Serilog.Exceptions) MIT License.
   - [serilog/serilog-formatting-compact: Compact JSON event format for Serilog](https://github.com/serilog/serilog-formatting-compact). Apache-2.0 License.
   - [serilog/serilog-sinks-console: Write log events to System.Console as text or JSON, with ANSI theme support](https://github.com/serilog/serilog-sinks-console). Apache-2.0 License.
   - [serilog-contrib/Serilog.Sinks.DelegatingText: A Serilog sink to emit formatted log events to a delegate.](https://github.com/serilog-contrib/Serilog.Sinks.DelegatingText). Apache License, Version 2.0.
 - [HamedFathi/SimMetricsCore: A text similarity metric library, e.g. from edit distance's (Levenshtein, Gotoh, Jaro, etc) to other metrics, (e.g Soundex, Chapman). This library is compiled based on the .NET standard with a lot of useful extension methods.](https://github.com/HamedFathi/SimMetricsCore)
 - [replaysMike/TypeSupport: A CSharp library that makes it easier to work with Types dynamically.](https://github.com/replaysMike/TypeSupport) - When working with generic and dynamic types I appreciate some of the extension methods provided by this library to handle details like .IsNumericType that often seem to descend into endless edge cases when you try to write it yourself. GPL-3.0 License.
 - [omuleanu/ValueInjecter: convention based mapper](https://github.com/omuleanu/ValueInjecter) - Quick mapping between objects without any setup needed. MIT License.
 - [Tyrrrz/CliWrap: Library for running command-line processes](https://github.com/Tyrrrz/CliWrap) - Improved options for running command line processes from .NET. MIT License.
 - [HangfireIO/Cronos: A fully-featured .NET library for working with Cron expressions. Built with time zones in mind and intuitively handles daylight saving time transitions](https://github.com/HangfireIO/Cronos). MIT License.
 - [bradymholt/cron-expression-descriptor: A .NET library that converts cron expressions into human readable descriptions.](https://github.com/bradymholt/cron-expression-descriptor). MIT License.

**Testing:**
- [GitHub - adobe/S3Mock: A simple mock implementation of the AWS S3 API startable as Docker image, TestContainer, JUnit 4 rule, JUnit Jupiter extension or TestNG listener](https://github.com/adobe/S3Mock#configuration) - One docker command to have a mock S3 server running and minimal configuration needed for simple cases!!! Apache License, Version 2.0.
