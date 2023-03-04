using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

/// <summary>
///     Interaction logic for MapComponentEditorWindow.xaml
/// </summary>
[ObservableObject]
public partial class MapComponentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private MapComponentEditorContext _mapComponentContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    /// <summary>
    /// DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    /// core functionality being uninitialized.
    /// </summary>
    private MapComponentEditorWindow()
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
    public static async Task<MapComponentEditorWindow> CreateInstance(MapComponent? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new MapComponentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.MapComponentContent = await MapComponentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.MapComponentContent.RequestContentEditorWindowClose += (_, _) =>
        {
            window.Dispatcher?.Invoke(window.Close);
        };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.MapComponentContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}