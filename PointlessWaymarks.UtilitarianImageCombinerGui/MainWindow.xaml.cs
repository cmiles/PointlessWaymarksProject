using Metalama.Patterns.Observability;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.UtilitarianImage;
using PointlessWaymarks.UtilitarianImageCombinerGui.Controls;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.ProgramUpdateMessage;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.UtilitarianImageCombinerGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[Observable]
[StaThreadConstructorGuard]
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        var versionInfo =
            ProgramInfoTools.StandardAppInformationString(AppContext.BaseDirectory,
                "Utilitarian Image Combiner Beta");

        InfoTitle = versionInfo.humanTitleString;

        var currentDateVersion = versionInfo.dateVersion;

        StatusContext = new StatusControlContext();

        DataContext = this;

        UpdateMessageContext = new ProgramUpdateMessageContext(StatusContext);

        HelpTabContext = new HelpDisplayContext([
            HelpText
        ]);

        StatusContext.RunFireAndForgetBlockingTask(Setup);

        StatusContext.RunFireAndForgetBlockingTask(async () => { await CheckForProgramUpdate(currentDateVersion); });
    }

    public AppSettingsContext? AppSettingsTabContext { get; set; }
    public CombinerListContext? CombinerTabContext { get; set; }
    public HelpDisplayContext HelpTabContext { get; set; }

    public string HelpText =>
        $"""
         ## Utilitarian Image Combiner

         This program combines images and pdfs into a single JPEG image. It is designed for utilitarian concerns like record keeping - aesthetic concerns are largely ignored.

         To add images or pdfs use the 'Add Images' button or drag and drop files into the list. You can drag and drop items to reorder the list, rotate and view images, and remove items.

         Images and pdfs can be combined into a Vertical, Horizontal, or Grid orientation. The maximum width and height for each image can be set and along with a JPEG quality setting gives you some control over the size of the final image.

         Supported Input File Extensions: {string.Join(", ", Combiner.SupportedExtensions)}

         ## Background

         For several years I have been generating private sites with the [Pointless Waymarks CMS](https://github.com/cmiles/PointlessWaymarksProject) to track personal items like camera gear, books and home purchases. For these sites it has been very useful to take several photos of an item with my phone (a photo of a lens showing the brand/model, another showing the serial number, maybe one showing the packaging, sometimes another with accessories...), and sometimes a PDF receipt, and combine them into a single image for record keeping. In the past I often did this on my Android phone with [ZomboDroid Image Combiner & Editor](https://play.google.com/store/apps/details?id=com.zombodroid.imagecombinerfree) - but over time as I did this more the photos were not always on my phone and I wanted to be able to also combine images of PDF receipts - so I created this program!

         {HelpMarkdown.CombinedAboutToolsAndPackages}
         """;

    public string InfoTitle { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public ProgramUpdateMessageContext UpdateMessageContext { get; set; }

    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        var settings = ImageCombinerGuiSettingTools.ReadSettings();

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = await ProgramInfoTools.LatestInstaller(
            settings.ProgramUpdateDirectory,
            "PointlessWaymarksUtilitarianImageCombiner-Setup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile ?? string.Empty}");

        await UpdateMessageContext.LoadData(currentDateVersion, dateString, setupFile);
    }

    public async Task Setup()
    {
        CombinerTabContext = await CombinerListContext.CreateInstance(StatusContext);
        AppSettingsTabContext = new AppSettingsContext();
    }
}