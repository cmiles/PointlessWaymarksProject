using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

/// <summary>
///     Interaction logic for GpxImportWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class GpxImportWindow
{
    public GpxImportContext ImportContext { get; set; }
    public StatusControlContext StatusContext { get; set; }

    private GpxImportWindow(StatusControlContext statusContext, GpxImportContext importContext)
    {
        InitializeComponent();
        StatusContext = statusContext;
        ImportContext = importContext;

        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed. Does not show the window - consider using
    /// PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<GpxImportWindow> CreateInstance(string? initialImportFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var statusContext = new StatusControlContext();

        var importContext = await GpxImportContext.CreateInstance(statusContext);

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new GpxImportWindow( statusContext, importContext);

        if (string.IsNullOrWhiteSpace(initialImportFile))
            await window.ImportContext.ChooseAndLoadFile();
        else
            await window.ImportContext.LoadFile(initialImportFile);

        return window;
    }
}