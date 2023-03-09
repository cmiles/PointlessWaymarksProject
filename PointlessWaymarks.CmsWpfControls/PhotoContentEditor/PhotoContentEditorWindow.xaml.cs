using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PhotoContentEditor;

[ObservableObject]
public partial class PhotoContentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private PhotoContentEditorContext _photoContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    /// <summary>
    /// DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    /// core functionality being uninitialized.
    /// </summary>
    private PhotoContentEditorWindow()
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
    public static async Task<PhotoContentEditorWindow> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new PhotoContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.PhotoContent = await PhotoContentEditorContext.CreateInstance(window.StatusContext);

        window.PhotoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.PhotoContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed. Does not show the window - consider using
    /// PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<PhotoContentEditorWindow> CreateInstance(FileInfo initialPhoto)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new PhotoContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.PhotoContent = await PhotoContentEditorContext.CreateInstance(window.StatusContext, initialPhoto);

        window.PhotoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.PhotoContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed. Does not show the window - consider using
    /// PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<PhotoContentEditorWindow> CreateInstance(PhotoContent? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new PhotoContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.PhotoContent = await PhotoContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.PhotoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.PhotoContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}