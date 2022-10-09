This code was pulled from [bcwood/PressSharper: A C# class library for parsing WordPress XML export data.](https://github.com/bcwood/PressSharper) which was forked from [dreadwail/press_sharp: A C# class library for parsing Wordpress XML export data.](https://github.com/dreadwail/press_sharp). It has been lightly refactored and integrated into this project.

WordPress is an amazing piece of software and an obvious choice for easily creating online content. For quite some time Wordpress has had an option to export your data as an XML file. PressSharper has mappings and data structures to consume an exported WordPress XML file and have a reasonable to work with representation of your data.

The easiest way to get started:
	var exportedData = new Blog(await File.ReadAllTextAsync("yourxmlfile"));

Notes:
 - WordPress is constantly changing - my use of PressSharp has been with some older installs, it is always possible that the latest and greatest WordPress versions change the XML (or no longer offer it) - use with caution...
 - In many cases a WordPress export might be 'technically correct' but incredibly disappointing to try to use outside the context of your WordPress install - media files from the WordPress library and some plugins can be tricky to deal with and PressSharper is not setup to do anything except help you wrangle the XML...
 - See the associated Test project!