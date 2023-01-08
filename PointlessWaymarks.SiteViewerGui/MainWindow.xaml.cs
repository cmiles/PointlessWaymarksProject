using System.IO;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Hosting;
using PointlessWaymarks.CmsWpfControls.SitePreview;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.SiteViewerGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow
{
    [ObservableProperty] private SitePreviewContext _previewContext;
    [ObservableProperty] private StatusControlContext _statusContext;

    public MainWindow()
    {
        InitializeComponent();

        StatusContext = new StatusControlContext { BlockUi = false };

        DataContext = this;
    }

    public MainWindow(string siteUrl, string localFolder, string siteName)
    {
        InitializeComponent();

        StatusContext = new StatusControlContext { BlockUi = false };

        DataContext = this;

        if (string.IsNullOrWhiteSpace(localFolder)) localFolder = Environment.CurrentDirectory;

        if (string.IsNullOrWhiteSpace(siteUrl) || string.IsNullOrWhiteSpace(siteName))
        {
            var possibleFile = Directory.EnumerateFiles(localFolder, "index.htm*").MinBy(x => x.Length);

            if (!string.IsNullOrWhiteSpace(possibleFile))
            {
                var urlFound = !string.IsNullOrWhiteSpace(siteUrl);
                var siteNameFound = !string.IsNullOrWhiteSpace(siteName);

                foreach (var loopLine in File.ReadLines(possibleFile))
                {
                    if (!urlFound)
                    {
                        var urlString = Regex
                            .Match(loopLine, "<meta property=\"og:url\" content=\"(?<contentUrl>.*)\">",
                                RegexOptions.IgnoreCase).Groups["contentUrl"].Value;

                        if (!string.IsNullOrWhiteSpace(urlString))
                        {
                            urlFound = true;
                            siteUrl = new Uri(urlString).Host;
                        }
                    }

                    if (!siteNameFound)
                    {
                        var siteNameString = Regex.Match(loopLine,
                            "<meta property=\"og:site_name\" content=\"(?<contentUrl>.*)\">",
                            RegexOptions.IgnoreCase).Groups["contentUrl"].Value;

                        if (!string.IsNullOrWhiteSpace(siteNameString))
                        {
                            siteNameFound = true;
                            siteName = siteNameString;
                        }
                    }

                    if (urlFound && siteNameFound) break;

                    if (loopLine.Contains("</head>", StringComparison.OrdinalIgnoreCase)) break;
                }
            }
        }

        var freePort = PreviewServer.FreeTcpPort();

        var server = PreviewServer.CreateHostBuilder(
            siteUrl, localFolder, freePort).Build();

        StatusContext.RunFireAndForgetWithToastOnError(async () =>
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            await server.RunAsync();
        });

        PreviewContext = new SitePreviewContext(siteUrl,
            localFolder,
            siteName, $"localhost:{freePort}", StatusContext);
    }
}