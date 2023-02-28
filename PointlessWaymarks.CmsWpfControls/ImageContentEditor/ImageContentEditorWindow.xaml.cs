using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.ImageContentEditor;

[ObservableObject]
public partial class ImageContentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private ImageContentEditorContext _imageEditor;
    [ObservableProperty] private StatusControlContext _statusContext;

    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private ImageContentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<ImageContentEditorWindow> CreateInstance(ImageContent contentToLoad = null,
        FileInfo initialImage = null)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ImageContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.ImageEditor =
            await ImageContentEditorContext.CreateInstance(window.StatusContext, contentToLoad, initialImage);

        window.ImageEditor.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.ImageEditor)
            {
                CloseAction = x => ((ImageContentEditorWindow)x).ImageEditor.Saved = null
            };

        return window;
    }
}