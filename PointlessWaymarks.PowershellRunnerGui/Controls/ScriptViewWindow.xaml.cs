using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

/// <summary>
///     Interaction logic for ScriptViewWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class ScriptViewWindow
{
    public ScriptViewWindow()
    {
        InitializeComponent();

        DataContext = this;
    }

    public ScriptViewContext? ScriptContext { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; } = string.Empty;

    public static async Task CreateInstance(Guid jobId, string databaseFile)
    {
        var factoryStatusContext = await StatusControlContext.CreateInstance();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var windowTitle = "Script Viewer";

        var factoryJobRunContext =
            await ScriptViewContext.CreateInstance(factoryStatusContext, jobId, databaseFile);

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ScriptViewWindow
        {
            StatusContext = factoryStatusContext,
            ScriptContext = factoryJobRunContext,
            WindowTitle = windowTitle
        };

        await window.PositionWindowAndShowOnUiThread();
    }
}