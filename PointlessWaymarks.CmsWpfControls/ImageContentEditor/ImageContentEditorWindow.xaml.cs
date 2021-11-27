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

    public ImageContentEditorWindow()
    {
        InitializeComponent();

        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            ImageEditor = await ImageContentEditorContext.CreateInstance(StatusContext);

            ImageEditor.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, ImageEditor);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }

    public ImageContentEditorWindow(ImageContent contentToLoad = null, FileInfo initialImage = null)
    {
        InitializeComponent();

        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            ImageEditor = await ImageContentEditorContext.CreateInstance(StatusContext, contentToLoad, initialImage);

            ImageEditor.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, ImageEditor);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }
}