using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PointContentEditor;

/// <summary>
///     Interaction logic for PointContentEditorWindow.xaml
/// </summary>
[ObservableObject]
public partial class PointContentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private PointContentEditorContext _pointContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    /// <summary>
    /// DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    /// core functionality being uninitialized.
    /// </summary>
    private PointContentEditorWindow()
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
    public static async Task <PointContentEditorWindow> CreateInstance(PointContent toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new PointContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.PointContent = await PointContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.PointContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.PointContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}