using System.Windows;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

/// <summary>
///     Interaction logic for ScriptJobRunListWindow.xaml
/// </summary>
public partial class ScriptJobRunListWindow
{
    public ScriptJobRunListWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public ScriptJobRunListContext? JobRunListContext { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; } = string.Empty;

    public static async Task CreateInstance(List<Guid> jobFilter, string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var windowTitle = "Script Job Run List";

        var factoryContext = new StatusControlContext();

        var factoryJobRunListContext =
            await ScriptJobRunListContext.CreateInstance(factoryContext, jobFilter, databaseFile);

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ScriptJobRunListWindow
        {
            StatusContext = factoryContext,
            JobRunListContext = factoryJobRunListContext,
            WindowTitle = windowTitle
        };

        await window.PositionWindowAndShowOnUiThread();
    }
}