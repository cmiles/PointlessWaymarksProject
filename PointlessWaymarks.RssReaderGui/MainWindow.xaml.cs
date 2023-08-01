using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.RssReaderGui.Controls;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.ProgramUpdateMessage;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.RssReaderGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class MainWindow
{
    public readonly string HelpText =
"""
## Pointless Waymarks RSS Reader

The Pointless Waymarks RSS Reader is a Windows Desktop (only!) Feed Reader. The program uses a SQLite database to store data about Feeds and Feed Items. The emphasis in this program is NOT displaying the RSS Content in a feed, but rather displaying the URL the Feed Links to.

There are a number of great options for RSS Readers - so why write another one is a good question...
 - Windows Desktop Only: After many years of RSS use my strong preference is that I don't want to read feeds all the time everywhere! Also I like: sitting in front of a desktop computer with a big screen (or screens!), desktop programs, owning my own data, keeping my data local and I like that I can't sit in front of the computer all day both because of 'life' and because I know how terrible that is for me...
 - Emphasize Displaying Linked Content Not the Feed Content: Feeds are just data and can be used in an awesome number of ways - but the convention is that a Feed Item links to content and I just want to see the content, in full... 
 - Simple Feed List: I wonder at this point if I have spent a full day of my life organizing and tweaking the display of Feeds/Folders in Feed Readers? Clicking/unclicking/manipulating tree like structures of Feeds... I'm interested in a simpler display of Feeds that removes the temptation to fiddle and presents fewer options.
 - Joy! I love the art and craft of writing software and I love the feeling of using software that directly addresses my needs/wants/workflow/ideas.

While the GUI, approach, vision, scope, design and nearly every detail is different this program will always be based on my memories of using [FeedDemon](https://nick.typepad.com/blog/2013/03/the-end-of-feeddemon.html) especially in the late 2000s!
""";

    public MainWindow()
    {
        InitializeComponent();
        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        var versionInfo =
            ProgramInfoTools.StandardAppInformationString(AppContext.BaseDirectory,
                "Pointless Waymarks Rss Reader Beta");

        InfoTitle = versionInfo.humanTitleString;

        var currentDateVersion = versionInfo.dateVersion;

        StatusContext = new StatusControlContext { BlockUi = false };

        DataContext = this;

        UpdateMessageContext = new ProgramUpdateMessageContext();

        HelpContext = new HelpDisplayContext(new List<string>
        {
            HelpText,
            HelpMarkdown.SoftwareUsedBlock
        });
        
        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            await CheckForProgramUpdate(currentDateVersion);

            await LoadData();
        });
    }

    public FeedListContext? FeedContext { get; set; }

    public HelpDisplayContext HelpContext { get; set; }
    public string InfoTitle { get; set; }
    public FeedItemListContext? ReaderContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public ProgramUpdateMessageContext UpdateMessageContext { get; set; }

    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        var settings = RssReaderGuiSettingTools.ReadSettings();

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = ProgramInfoTools.LatestInstaller(
            settings.ProgramUpdateDirectory,
            "PointlessWaymarksRssReaderSetup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile?.FullName ?? string.Empty}");

        if (string.IsNullOrWhiteSpace(dateString) || setupFile is not { Exists: true }) return;

        if (string.Compare(currentDateVersion, dateString, StringComparison.OrdinalIgnoreCase) >= 0) return;

        await UpdateMessageContext.LoadData(currentDateVersion, dateString, setupFile);
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ReaderContext = await FeedItemListContext.CreateInstance(StatusContext);
        FeedContext = await FeedListContext.CreateInstance(StatusContext);
    }
}