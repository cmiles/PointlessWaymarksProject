using System.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.ImageContentEditor;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class ImageContentEditorWindow
{
    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private ImageContentEditorWindow()
    {
        InitializeComponent();
        DataContext = this;
        WindowTitle = $"Image Editor - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public ImageContentEditorContext? ImageEditor { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<ImageContentEditorWindow> CreateInstance(ImageContent? contentToLoad = null,
        FileInfo? initialImage = null, bool positionAndShowWindow = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ImageContentEditorWindow { StatusContext = await StatusControlContext.CreateInstance() };

        if (positionAndShowWindow) window.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.ImageEditor =
            await ImageContentEditorContext.CreateInstance(window.StatusContext, contentToLoad, initialImage);

        window.WindowTitle =
            $"Image Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.ImageEditor.TitleSummarySlugFolder.TitleEntry.UserValue}";

        window.ImageEditor.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, _) =>
        {
            window.WindowTitle =
                $"Image Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.ImageEditor.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.ImageEditor.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.ImageEditor)
            {
                CloseAction = x =>
                {
                    var imageEditor = ((ImageContentEditorWindow)x).ImageEditor;
                    if (imageEditor != null) imageEditor.Saved = null;
                }
            };

        return window;
    }
}