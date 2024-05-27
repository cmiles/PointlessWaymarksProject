namespace PointlessWaymarks.CommonTools;

public static class HelpMarkdown
{
    public static string PointlessWaymarksAllProjectsQuickDescription =
        """
        ## The Pointless Waymarks Project
        
        The Pointless Waymarks Project is a set of applications that are connected by a focus on durable, rich, low maintenance, free, long-lasting, organized content and stories about the landscape, life, place, history and nature.
          - [Windows Desktop CMS, Static Site Generator, Offline Site Viewer and supporting Command Line Tasks](https://github.com/cmiles/PointlessWaymarksProject) - see [Pointless Waymarks](https://pointlesswaymarks.com/) and [cmiles - info](https://www.cmiles.info/), both sites are created with this system.
          - [Pointless Waymarks GeoTools](https://github.com/cmiles/PointlessWaymarksProject/blob/main/PointlessWaymarks.GeoToolsGui/README.md) - a Windows Desktop program to help with: geotagging Photos (both from gpx files and Garmin Connect), tagging geotagged photos with location information (state, park/monument name, etc) and downloading activities from Garmin Connect.
          - [Pointless Waymarks Cloud Backup](https://github.com/cmiles/PointlessWaymarksProject/blob/main/PointlessWaymarks.CloudBackupGui/README.md) - a data focused Windows backup program that uses Amazon S3 or Cloudflare R2. Features include: MD5 file comparisons, a maximum runtime for backups, Excel reports and easy scheduling via the Windows Task Scheduler. This program does not created versioned backups or have an mechanism to restore your files -> instead it focuses on creating a simple/direct backup of your files on S3/R2 - with your files on S3/R2 you can easily explore your backup on the web or via free clients on nearly any OS and in an emergency there is a very good chance you can find some reasonable way to see/view/access your backups.
          - [Pointless Waymarks Feed Reader](https://github.com/cmiles/PointlessWaymarksProject/blob/main/PointlessWaymarks.FeedReaderGui/README.MD) - a simple Windows Desktop Feed Reader. No folders to organize, no online account and no temptation to doom scroll on your phone - this is a Windows Desktop only program for reading content that you care about with very low setup and ceremony.
          - [Open Source Code on GitHub](https://github.com/cmiles/PointlessWaymarksProject): The source code for the project is available on GitHub and (largely) MIT Licensed. Primary technologies include C#, .NET Core, WPF, EF Core, WebView2 and SQLite. The project has projects/components/details that might be generic enough to be re-used in your own code...
        """;

    public static string SoftwareUsedBlock =>
        """
### Software and Tools Used by the Pointless Waymarks Project

I am incredibly grateful to the all the people and projects that make it possible to rapidly build useful, open, low/no-cost software. Below is a mostly-up-to-date-and-largely-comprehensive list of tools/packages/libraries/etc. that are used to build the [Pointless Waymarks Project](https://github.com/cmiles/PointlessWaymarksProject):

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

**Testing:**
 - [GitHub - adobe/S3Mock: A simple mock implementation of the AWS S3 API startable as Docker image, TestContainer, JUnit 4 rule, JUnit Jupiter extension or TestNG listener](https://github.com/adobe/S3Mock#configuration) - One docker command to have a mock S3 server running and minimal configuration needed for simple cases!!! Apache License, Version 2.0.
""";
}