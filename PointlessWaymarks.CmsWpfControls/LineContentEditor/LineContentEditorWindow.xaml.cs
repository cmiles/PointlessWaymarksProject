using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.LineContentEditor;

/// <summary>
///     Interaction logic for LineContentEditorWindow.xaml
/// </summary>
[ObservableObject]
#pragma warning disable MVVMTK0033
public partial class LineContentEditorWindow
#pragma warning restore MVVMTK0033
{
    [ObservableProperty] private WindowAccidentalClosureHelper? _accidentalCloserHelper;
    [ObservableProperty] private LineContentEditorContext? _lineContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    /// <summary>
    /// DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    /// core functionality being uninitialized.
    /// </summary>
    private LineContentEditorWindow()
    {
        InitializeComponent();
        _statusContext = new StatusControlContext();
        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed. Does not show the window - consider using
    /// PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<LineContentEditorWindow> CreateInstance(LineContent? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new LineContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.LineContent = await LineContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.LineContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.LineContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}