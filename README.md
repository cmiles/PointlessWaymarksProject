# Pointless Waymarks CMS

This project is not truly intended for 'general public' use - it is made public under an MIT license both to share with friends and in case any of the code is useful to anyone.

This project is a .NET Core Windows WPF GUI for generating a static website based on database information and a settings file. This software is currently being used to create https://PointlessWaymarks.com. It is not intended to be a starting point for 'general static site generation software' - it is designed instead to support the creation and generation of a very specific set of content types that I am currently interested in.

Because this is a personal project without commercial goals it may be useful to understand that after many years of creating and having personal content online my only current motivation is to create long-lasting beautiful free content with love - creating content has brought me incredible joy, popularity and having front page search results have not added any measurable happiness to my life.

Details behind this software:
 - Static Files because I believe that this is currently the lowest cost, least maintenance, most durable way to put free content online.
 - Generated and database driven because it offers a huge amount of flexibility in updating and creating content and leaves open the possibility of generating different formats.
 - Custom software because I believe I will be working with the same content types over and over again and after years of creating content in 'generic' editors I now want highly specialized no compromise ways to create exactly what I want.
 - Custom software because I believe that my goals are fundamentally simple enough that I can create new software that brings me joy with less effort and time than it would take to customize existing software.
 - I hope to write high quality code and I am happy to share it - but for various reason, including the incredibly narrow and personal focus of this project, I don't currently intend to make 'public use' a consideration

Todo Lists, Idea Lists and a development log are [found in a DevNotes.md file that is versioned along with the code](PointlessWaymarksCmsContentEditor/DevNotes.md).

If you have questions or comments you are certainly welcome to contact me at PointlessWaymarks@gmail.com


### Application Screen Shots

Launch screen with recent files and option to create a new project.

![Launch Screen](PointlessWaymarksCmsScreenShots/LaunchScreen.jpg "Launch Screen")


Post List - There is a similiar list for each Content Type.

![Posts List](PointlessWaymarksCmsScreenShots/PostsList.jpg "Posts List")


Post Editor with Preview Showing - The editors, like the entire application - is relatively simple but quick and easy to work with.

![Post Editor](PointlessWaymarksCmsScreenShots/PostEditorWithPreview.jpg "Post Editor")


The first tab of the Photo Content Editor - most of the data on this screen was imported from the photo metadata.

![Photo Content Editor](PointlessWaymarksCmsScreenShots/PhotoEditor.jpg "Photo Content Editor")


### Software Used By and In Building Pointless Waymarks CMS

I am incredibly greatful to the all the people and companies who have created the software that allows me to easily create software like Pointless Waymarks CMS- here are some of the packages and software that I am currently using:
 - [Visual Studio IDE, Code Editor, Azure DevOps, & App Center - Visual Studio](https://visualstudio.microsoft.com/), [.NET Core (Linux, macOS, and Windows)](https://dotnet.microsoft.com/download/dotnet-core)
 - [ReSharper: The Visual Studio Extension for .NET Developers by JetBrains](https://www.jetbrains.com/resharper/)
 - [SQLite](https://www.sqlite.org/index.html)
  - [dotnet/efcore: EF Core is a modern object-database mapper for .NET. It supports LINQ queries, change tracking, updates, and schema migrations.](https://github.com/dotnet/efcore) and various other Microsoft Technologies
 - [drewnoakes/metadata-extractor-dotnet: Extracts Exif, IPTC, XMP, ICC and other metadata from image, video and audio files](https://github.com/drewnoakes/metadata-extractor-dotnet) - Used to read the metadata in Photographs - there are a number of ways to get this data but it is nice to have a single go to library to work with that already handles a number of the (many...) issues. Apache License, Version 2.0.
 - [mokacao/PhotoSauce: MagicScaler high-performance, high-quality image processing pipeline for .NET](https://github.com/mokacao/PhotoSauce) - Fast high quality Image Resizing. Ms-Pl.
 - [AngleSharp - Home](https://anglesharp.github.io/) - [AngleSharp/AngleSharp: The ultimate angle brackets parser library parsing HTML5, MathML, SVG and CSS to construct a DOM based on the official W3C specifications.](https://github.com/AngleSharp/AngleSharp) - Mainly used for parsing web pages when creating links. MIT License.
 - [fluentmigrator/fluentmigrator: Fluent migrations framework for .NET](https://github.com/fluentmigrator/fluentmigrator) -  documentation](https://fluentmigrator.github.io/)
 - [MartinTopfstedt/FontAwesome5: WPF controls for the iconic SVG, font, and CSS toolkit Font Awesome 5.](https://github.com/MartinTopfstedt/FontAwesome5) - a quick and easy way to use [Font Awesome](https://fontawesome.com/) icons in WPF. MIT License.
 - [HtmlTags/htmltags: Simple object model for generating HTML](https://github.com/HtmlTags/htmltags) - Currently this project uses a combination of T4 templates and tags built by this library to produce HTML. Apache License, Version 2.0.
 - [anakic/Jot: Jot is a library for persisting and applying .NET application state.](https://github.com/anakic/Jot) - Used to save application state most prominently main window position.
 - [lunet-io/markdig: A fast, powerful, CommonMark compliant, extensible Markdown processor for .NET](https://github.com/lunet-io/markdig) and [Kryptos-FR/markdig.wpf: A WPF library for lunet-io/markdig https://github.com/lunet-io/markdig](https://github.com/Kryptos-FR/markdig.wpf) - Used to process Commonmark Markdown both inside the application and for HTML generation. BSD 2-Clause ""Simplified"" License and MIT License.
 - [thomasgalliker/ObjectDumper: ObjectDumper is a utility which aims to serialize C# objects to string for debugging and logging purposes.](https://github.com/thomasgalliker/ObjectDumper) - A quick way to convert objects to human readable strings/formats. Apache License, Version 2.0
 - [acemod13/ookii-dialogs-wpf: Common dialog classes for WPF applications](https://github.com/acemod13/ookii-dialogs-wpf) - easy access to several nice dialogs. [License of Ookii.Dialogs.Wpf.NETCore 2.1.0](https://www.nuget.org/packages/Ookii.Dialogs.Wpf.NETCore/2.1.0/License).
 - [thomaslevesque/WeakEvent: Generic weak event implementation](https://github.com/thomaslevesque/WeakEvent/) - Apache License 2.0.
 - [omuleanu/ValueInjecter: convention based mapper](https://github.com/omuleanu/ValueInjecter) - Quick mapping between objects without any setup needed. MIT License.
 - [micdenny/WpfScreenHelper: Porting of Windows Forms Screen helper for Windows Presentation Foundation (WPF). It avoids dependencies on Windows Forms libraries when developing in WPF.](https://github.com/micdenny/WpfScreenHelper) - help with some details of keeping windows in visible screen space without referencing WinForms. MIT License.
 - [kzu/GitInfo: Git and SemVer Info from MSBuild, C# and VB](https://github.com/kzu/GitInfo) - Git version information. MIT License.
 - [pinboard.net/LICENSE at master · shrayasr/pinboard.net](https://github.com/shrayasr/pinboard.net/blob/master/LICENSE) - Easy to use wrapper for [Pinboard - 'Social Bookmarking for Introverts'](http://pinboard.in/). MIT License.
 - [jamesmontemagno/mvvm-helpers: Collection of MVVM helper classes for any application](https://github.com/jamesmontemagno/mvvm-helpers). MIT License.
 - [shps951023/HtmlTableHelper: Mini C# IEnumerable object to HTML Table String Library](https://github.com/shps951023/HtmlTableHelper) - used for quick reporting output like the Photo Metadata Dump. MIT License.
 - [Pure](https://purecss.io/) - Used in the reporting output for simple styling. BSD and MIT Licenses.
 - [Material Design Icons](http://materialdesignicons.com/)
 - [ClosedXML](https://github.com/ClosedXML/ClosedXML) - A great way to read and write Excel Files - I have years of experience with this library and it is both excellent and well maintained. MIT License.
  - [CompareNETObjects](https://github.com/GregFinzer/Compare-Net-Objects) - Comparison of object properties that stays quick/easy to use but has more options than you would be likely to create with custom reflection code and potentially more durability than hand coded comparisons. Ms-PL License.