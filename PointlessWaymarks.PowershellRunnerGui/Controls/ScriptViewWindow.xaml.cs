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
        await ThreadSwitcher.ResumeBackgroundAsync();

        var windowTitle = "Script Viewer";

        var factoryContext = new StatusControlContext();

        var factoryJobRunContext =
            await ScriptViewContext.CreateInstance(factoryContext, jobId, databaseFile);

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ScriptViewWindow
        {
            StatusContext = factoryContext,
            ScriptContext = factoryJobRunContext,
            WindowTitle = windowTitle
        };

        await window.PositionWindowAndShowOnUiThread();
    }
}