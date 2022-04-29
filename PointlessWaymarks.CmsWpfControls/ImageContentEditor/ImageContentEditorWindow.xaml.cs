using System.IO;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.ImageContentEditor;

[ObservableObject]
public partial class ImageContentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private ImageContentEditorContext _imageEditor;
    [ObservableProperty] private StatusControlContext _statusContext;

    private ImageContentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

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
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.ImageEditor);

        return window;
    }
}