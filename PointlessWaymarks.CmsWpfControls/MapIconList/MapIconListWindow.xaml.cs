using System.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.MapIconList;

/// <summary>
///     Interaction logic for MapIconListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class MapIconListWindow
{
    private bool _closeConfirmed;

    public MapIconListWindow(MapIconListContext listContext, StatusControlContext statusContext)
    {
        InitializeComponent();

        StatusContext = statusContext;
        ListContext = listContext;
        WindowTitle = $"Map Icon List - {UserSettingsSingleton.CurrentSettings().SiteName}";
        DataContext = this;

        Closing += Window_OnClosing;
    }

    public MapIconListContext ListContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<MapIconListWindow> CreateInstance()
    {
        var factoryStatusContext = await StatusControlContext.CreateInstance();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = await MapIconListContext.CreateInstance(factoryStatusContext);

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new MapIconListWindow(factoryContext, factoryStatusContext);

        factoryStatusContext.RunFireAndForgetBlockingTask(factoryContext.LoadData);

        return window;
    }

    private void Window_OnClosing(object? sender, CancelEventArgs e)
    {
        if (_closeConfirmed) return;

        e.Cancel = true;

        StatusContext.RunFireAndForgetBlockingTask(WindowClosing);
    }

    private async Task WindowClosing()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!ListContext.Items.Any(x => x.HasChanges))
        {
            _closeConfirmed = true;
            await ThreadSwitcher.ResumeForegroundAsync();
            Close();
        }

        if (await StatusContext.ShowMessage("Unsaved Changes...",
                "There are unsaved changes - do you want to discard your changes?",
                ["Yes - Close Window", "No"]) == "Yes - Close Window")
        {
            _closeConfirmed = true;
            await ThreadSwitcher.ResumeForegroundAsync();
            Close();
        }
    }
}