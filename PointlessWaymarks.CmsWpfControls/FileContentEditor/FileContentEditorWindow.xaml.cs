using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.FileContentEditor;

[ObservableObject]
public partial class FileContentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private FileContentEditorContext _fileContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    /// <summary>
    /// DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    /// core functionality being uninitialized.
    /// </summary>
    private FileContentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<FileContentEditorWindow> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new FileContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.FileContent = await FileContentEditorContext.CreateInstance(window.StatusContext);

        window.FileContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.FileContent);

        return window;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<FileContentEditorWindow> CreateInstance(FileInfo initialFile)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new FileContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.FileContent = await FileContentEditorContext.CreateInstance(window.StatusContext, initialFile);

        window.FileContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.FileContent);

        return window;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed. Does not show the window - consider using
    /// PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<FileContentEditorWindow> CreateInstance(FileContent toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new FileContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.FileContent = await FileContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.FileContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.FileContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}