using Metalama.Patterns.Observability;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
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

        UpdateMessageContext = new ProgramUpdateMessageContext();

        StatusContext.RunFireAndForgetBlockingTask(Setup);

        StatusContext.RunFireAndForgetBlockingTask(async () => { await CheckForProgramUpdate(currentDateVersion); });
    }

    public string InfoTitle { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public HelpDisplayContext? HelpContext { get; set; }
    public ProgramUpdateMessageContext UpdateMessageContext { get; set; }
    public CombinerListContext? CombinerContext { get; set; }

    public async Task Setup()
    {
        CombinerContext = await CombinerListContext.CreateInstance(StatusContext);
    }

    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        var settings = ImageCombinerGuiSettingTools.ReadSettings();

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = ProgramInfoTools.LatestInstaller(
            settings.ProgramUpdateDirectory,
            "PointlessWaymarksUtilitarianImageCombinerSetup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile?.FullName ?? string.Empty}");

        if (string.IsNullOrWhiteSpace(dateString) || setupFile is not { Exists: true }) return;

        if (string.Compare(currentDateVersion, dateString, StringComparison.OrdinalIgnoreCase) >= 0) return;

        await UpdateMessageContext.LoadData(currentDateVersion, dateString, setupFile);
    }
}