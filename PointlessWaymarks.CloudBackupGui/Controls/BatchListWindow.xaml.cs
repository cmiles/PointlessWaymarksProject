using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class BatchListWindow
{
    public BatchListWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public BatchListContext? BatchContext { get; set; }
    public string JobName { get; set; } = string.Empty;
    public required StatusControlContext StatusContext { get; set; }

    public static async Task<BatchListWindow> CreateInstanceAndShow(int jobId,
        string jobName)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryStatusContext = await StatusControlContext.CreateInstance();
        factoryStatusContext.BlockUi = true;

        var window = new BatchListWindow
        {
            StatusContext = factoryStatusContext,
            JobName = jobName
        };

        window.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.StatusContext.Progress("Batch List - Creating Context");

        window.BatchContext =
            await BatchListContext.CreateInstance(window.StatusContext, jobId);

        window.BatchContext.CloseWindowRequest += (sender, args) => window.Close();

        window.StatusContext.BlockUi = false;

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}