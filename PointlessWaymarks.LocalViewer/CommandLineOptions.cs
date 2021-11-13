using CommandLine;

namespace PointlessWaymarks.LocalViewer;

public class CommandLineOptions
{
    [Option('u', "url", Required = true, HelpText = "The URL of the site to browse locally - www.pointlesswaymarks.com")]
    public string Url { get; set; }

    [Option('f', "folder", Required = false, HelpText = "The local root folder that contains the root content of the site to browse locally - C:\\PointlessWaymarks\\GeneratedSite - if not specified the directory the program is launched from will be used")]
    public string Folder { get; set; }

    [Option('s', "sitename", Required = false, HelpText = "The name of the site to browse - optional")]
    public string SiteName { get; set; }
}