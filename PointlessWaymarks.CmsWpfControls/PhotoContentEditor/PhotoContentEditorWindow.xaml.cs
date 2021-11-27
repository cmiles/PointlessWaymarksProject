using System.IO;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PhotoContentEditor;

[ObservableObject]
public partial class PhotoContentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private PhotoContentEditorContext _photoEditor;
    [ObservableProperty] private StatusControlContext _statusContext;

    public PhotoContentEditorWindow()
    {
        InitializeComponent();

        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            PhotoEditor = await PhotoContentEditorContext.CreateInstance(StatusContext);

            PhotoEditor.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PhotoEditor);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }

    public PhotoContentEditorWindow(FileInfo initialPhoto)
    {
        InitializeComponent();

        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            PhotoEditor = await PhotoContentEditorContext.CreateInstance(StatusContext, initialPhoto);

            PhotoEditor.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PhotoEditor);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }

    public PhotoContentEditorWindow(PhotoContent toLoad)
    {
        InitializeComponent();

        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            PhotoEditor = await PhotoContentEditorContext.CreateInstance(StatusContext, toLoad);

            PhotoEditor.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PhotoEditor);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }
}