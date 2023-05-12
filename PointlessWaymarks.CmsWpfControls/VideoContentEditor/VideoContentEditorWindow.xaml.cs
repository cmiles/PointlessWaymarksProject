using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.VideoContentEditor;

[ObservableObject]
#pragma warning disable MVVMTK0033
public partial class VideoContentEditorWindow
#pragma warning restore MVVMTK0033
{
    [ObservableProperty] private WindowAccidentalClosureHelper? _accidentalCloserHelper;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private VideoContentEditorContext? _videoContent;

    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private VideoContentEditorWindow()
    {
        InitializeComponent();
        _statusContext = new StatusControlContext();
        DataContext = this;
    }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<VideoContentEditorWindow> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new VideoContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.VideoContent = await VideoContentEditorContext.CreateInstance(window.StatusContext);

        window.VideoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.VideoContent)
            {
                CloseAction = x =>
                {
                    var videoContent = x as VideoContentEditorWindow;
                    videoContent?.VideoContent?.MainImageExternalEditorWindowCleanup();
                }
            };

        return window;
    }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<VideoContentEditorWindow> CreateInstance(FileInfo initialFile)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new VideoContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.VideoContent = await VideoContentEditorContext.CreateInstance(window.StatusContext, initialFile);

        window.VideoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.VideoContent)
            {
                CloseAction = x =>
                {
                    var videoContent = x as VideoContentEditorWindow;
                    videoContent?.VideoContent?.MainImageExternalEditorWindowCleanup();
                }
            };

        return window;
    }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<VideoContentEditorWindow> CreateInstance(VideoContent? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new VideoContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.VideoContent = await VideoContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.VideoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.VideoContent)
            {
                CloseAction = x =>
                {
                    var videoContent = x as VideoContentEditorWindow;
                    videoContent?.VideoContent?.MainImageExternalEditorWindowCleanup();
                }
            };

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}