using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

/// <summary>
///     Interaction logic for GpxImportWindow.xaml
/// </summary>
[ObservableObject]
public partial class GpxImportWindow
{
    [ObservableProperty] private GpxImportContext _importContext;
    [ObservableProperty] private StatusControlContext _statusContext;

    private GpxImportWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed. Does not show the window - consider using
    /// PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<GpxImportWindow> CreateInstance(string initialImportFile)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new GpxImportWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.ImportContext = await GpxImportContext.CreateInstance(window.StatusContext);

        if (string.IsNullOrWhiteSpace(initialImportFile))
            await window.ImportContext.ChooseAndLoadFile();
        else
            await window.ImportContext.LoadFile(initialImportFile);

        return window;
    }
}