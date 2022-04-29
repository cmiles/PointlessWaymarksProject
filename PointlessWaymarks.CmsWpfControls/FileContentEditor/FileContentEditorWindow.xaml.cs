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
        DataContext = this;
    }

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