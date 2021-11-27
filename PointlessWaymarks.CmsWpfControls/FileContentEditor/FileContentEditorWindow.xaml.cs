using System.IO;
using Microsoft.Toolkit.Mvvm.ComponentModel;
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

    public FileContentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            FileContent = await FileContentEditorContext.CreateInstance(StatusContext);

            FileContent.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, FileContent);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }

    public FileContentEditorWindow(FileInfo initialFile)
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            FileContent = await FileContentEditorContext.CreateInstance(StatusContext, initialFile);

            FileContent.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, FileContent);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }

    public FileContentEditorWindow(FileContent toLoad)
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            FileContent = await FileContentEditorContext.CreateInstance(StatusContext, toLoad);

            FileContent.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, FileContent);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }
}