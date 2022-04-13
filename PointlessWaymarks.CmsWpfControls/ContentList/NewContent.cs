using System.IO;
using System.Windows.Shell;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;
using PointlessWaymarks.CmsWpfControls.GeoJsonContentEditor;
using PointlessWaymarks.CmsWpfControls.GpxImport;
using PointlessWaymarks.CmsWpfControls.ImageContentEditor;
using PointlessWaymarks.CmsWpfControls.LineContentEditor;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.CmsWpfControls.MapComponentEditor;
using PointlessWaymarks.CmsWpfControls.NoteContentEditor;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.PostContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

[ObservableObject]
public partial class NewContent
{
    [ObservableProperty] private StatusControlContext _statusContext;

    public NewContent(StatusControlContext statusContext, WindowIconStatus windowStatus = null)
    {
        StatusContext = statusContext;
        WindowStatus = windowStatus;

        NewFileContentCommand = StatusContext.RunNonBlockingTaskCommand(NewFileContent);
        NewFileContentFromFilesCommand = StatusContext.RunBlockingTaskWithCancellationCommand(
            async x =>
            {
                await WindowIconStatus.IndeterminateTask(WindowStatus, async () => await NewFileContentFromFiles(x),
                    StatusContext.StatusControlContextId);
            }, "Cancel File Import");
        NewGeoJsonContentCommand = StatusContext.RunNonBlockingTaskCommand(NewGeoJsonContent);
        NewImageContentCommand = StatusContext.RunNonBlockingTaskCommand(NewImageContent);
        NewImageContentFromFilesCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(async x => await NewImageContentFromFiles(x),
                "Cancel Image Import");
        NewLineContentCommand = StatusContext.RunNonBlockingTaskCommand(NewLineContent);
        NewLineContentFromFilesCommand = StatusContext.RunBlockingTaskWithCancellationCommand(
            async x =>
            {
                await WindowIconStatus.IndeterminateTask(WindowStatus,
                    async () => await NewLineContentFromFiles(x), StatusContext.StatusControlContextId);
            }, "Cancel Line Import");
        NewLinkContentCommand = StatusContext.RunNonBlockingTaskCommand(NewLinkContent);
        NewMapContentCommand = StatusContext.RunNonBlockingTaskCommand(NewMapContent);
        NewNoteContentCommand = StatusContext.RunNonBlockingTaskCommand(NewNoteContent);
        NewPhotoContentCommand = StatusContext.RunNonBlockingTaskCommand(NewPhotoContent);
        NewPhotoContentFromFilesCommand = StatusContext.RunBlockingTaskWithCancellationCommand(
            async x =>
            {
                await WindowIconStatus.IndeterminateTask(WindowStatus,
                    async () => await NewPhotoContentFromFiles(false, x), StatusContext.StatusControlContextId);
            }, "Cancel Photo Import");

        NewPhotoContentFromFilesWithAutosaveCommand = StatusContext.RunBlockingTaskWithCancellationCommand(
            async x =>
            {
                await WindowIconStatus.IndeterminateTask(WindowStatus,
                    async () => await NewPhotoContentFromFiles(true, x), StatusContext.StatusControlContextId);
            }, "Cancel Photo Import");
        NewPointContentCommand = StatusContext.RunNonBlockingTaskCommand(NewPointContent);
        NewPostContentCommand = StatusContext.RunNonBlockingTaskCommand(NewPostContent);

        NewGpxImportWindow = StatusContext.RunNonBlockingTaskCommand(NewGpxImport);
    }

    public RelayCommand NewFileContentCommand { get; }

    public RelayCommand NewFileContentFromFilesCommand { get; }

    public RelayCommand NewGeoJsonContentCommand { get; }

    public RelayCommand NewGpxImportWindow { get; set; }

    public RelayCommand NewImageContentCommand { get; }

    public RelayCommand NewImageContentFromFilesCommand { get; }

    public RelayCommand NewLineContentCommand { get; }

    public RelayCommand NewLineContentFromFilesCommand { get; set; }

    public RelayCommand NewLinkContentCommand { get; }

    public RelayCommand NewMapContentCommand { get; }

    public RelayCommand NewNoteContentCommand { get; }

    public RelayCommand NewPhotoContentCommand { get; }

    public RelayCommand NewPhotoContentFromFilesCommand { get; }

    public RelayCommand NewPhotoContentFromFilesWithAutosaveCommand { get; }

    public RelayCommand NewPointContentCommand { get; }

    public RelayCommand NewPostContentCommand { get; }

    public WindowIconStatus WindowStatus { get; set; }

    public async Task NewFileContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = new FileContentEditorWindow();

        newContentWindow.PositionWindowAndShow();
    }

    public async Task NewFileContentFromFiles(CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting File load.");

        var dialog = new VistaOpenFileDialog { Multiselect = true };

        if (!(dialog.ShowDialog() ?? false)) return;

        var selectedFiles = dialog.FileNames?.ToList() ?? new List<string>();

        if (!selectedFiles.Any()) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (selectedFiles.Count > 20)
        {
            StatusContext.ToastError($"Sorry - max limit is 20 files at once, {selectedFiles.Count} selected...");
            return;
        }

        var selectedFileInfos = selectedFiles.Select(x => new FileInfo(x)).ToList();

        if (!selectedFileInfos.Any(x => x.Exists))
        {
            StatusContext.ToastError("Files don't exist?");
            return;
        }

        selectedFileInfos = selectedFileInfos.Where(x => x.Exists).ToList();

        foreach (var loopFile in selectedFileInfos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ThreadSwitcher.ResumeForegroundAsync();

            var editor = new FileContentEditorWindow(loopFile);
            editor.PositionWindowAndShow();

            StatusContext.Progress($"New File Editor - {loopFile.FullName} ");

            await ThreadSwitcher.ResumeBackgroundAsync();
        }
    }

    public async Task NewGeoJsonContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = new GeoJsonContentEditorWindow(null);

        newContentWindow.PositionWindowAndShow();
    }

    public async Task NewGpxImport()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = new GpxImportWindow(null);

        newContentWindow.PositionWindowAndShow();
    }

    public async Task NewImageContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = new ImageContentEditorWindow();

        newContentWindow.PositionWindowAndShow();
    }

    public async Task NewImageContentFromFiles(CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting Image load.");

        var dialog = new VistaOpenFileDialog { Multiselect = true };

        if (!(dialog.ShowDialog() ?? false)) return;

        var selectedFiles = dialog.FileNames?.ToList() ?? new List<string>();

        if (!selectedFiles.Any()) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (selectedFiles.Count > 20)
        {
            StatusContext.ToastError($"Sorry - max limit is 20 files at once, {selectedFiles.Count} selected...");
            return;
        }

        var selectedFileInfos = selectedFiles.Select(x => new FileInfo(x)).ToList();

        if (!selectedFileInfos.Any(x => x.Exists))
        {
            StatusContext.ToastError("Files don't exist?");
            return;
        }

        selectedFileInfos = selectedFileInfos.Where(x => x.Exists).ToList();

        if (!selectedFileInfos.Any(FileHelpers.ImageFileTypeIsSupported))
        {
            StatusContext.ToastError("None of the files appear to be supported file types...");
            return;
        }

        if (selectedFileInfos.Any(x => !FileHelpers.ImageFileTypeIsSupported(x)))
            StatusContext.ToastWarning(
                $"Skipping - not supported - {string.Join(", ", selectedFileInfos.Where(x => !FileHelpers.ImageFileTypeIsSupported(x)))}");

        foreach (var loopFile in selectedFileInfos.Where(FileHelpers.ImageFileTypeIsSupported))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ThreadSwitcher.ResumeForegroundAsync();

            var editor = new ImageContentEditorWindow(initialImage: loopFile);
            editor.PositionWindowAndShow();

            StatusContext.Progress($"New Image Editor - {loopFile.FullName} ");

            await ThreadSwitcher.ResumeBackgroundAsync();
        }
    }

    public async Task NewLineContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = new LineContentEditorWindow(null);

        newContentWindow.PositionWindowAndShow();
    }

    public async Task NewLineContentFromFiles(CancellationToken cancellationToken)
    {
        await NewLineContentFromFiles(cancellationToken, StatusContext, WindowStatus);
    }

    public static async Task NewLineContentFromFiles(CancellationToken cancellationToken,
        StatusControlContext statusContext, WindowIconStatus windowStatus)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        statusContext.Progress("Starting Line load.");

        var dialog = new VistaOpenFileDialog { Multiselect = true };

        if (!(dialog.ShowDialog() ?? false)) return;

        var selectedFiles = dialog.FileNames?.ToList() ?? new List<string>();

        if (!selectedFiles.Any()) return;

        if (selectedFiles.Count > 10)
        {
            statusContext.ToastError("Opening new content in an editor window is limited to 10 files at a time...");
            return;
        }

        await ThreadSwitcher.ResumeBackgroundAsync();

        var selectedFileInfos = selectedFiles.Select(x => new FileInfo(x)).ToList();

        if (!selectedFileInfos.Any(x => x.Exists))
        {
            statusContext.ToastError("Files don't exist?");
            return;
        }

        selectedFileInfos = selectedFileInfos.Where(x => x.Exists).ToList();

        await NewLineContentFromFiles(selectedFileInfos, cancellationToken, statusContext, windowStatus);
    }

    public static async Task NewLineContentFromFiles(List<FileInfo> selectedFileInfos,
        CancellationToken cancellationToken,
        StatusControlContext statusContext, WindowIconStatus windowStatus)
    {
        var outerLoopCounter = 0;

        foreach (var loopFile in selectedFileInfos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            outerLoopCounter++;

            windowStatus?.AddRequest(new WindowIconStatusRequest(statusContext.StatusControlContextId,
                TaskbarItemProgressState.Normal, (decimal)outerLoopCounter / (selectedFileInfos.Count + 1)));

            var tracksList = await SpatialHelpers.TracksFromGpxFile(loopFile, statusContext.ProgressTracker());

            if (tracksList.Count < 1)
            {
                statusContext.ToastWarning($"No Tracks in {loopFile.Name}? Skipping...");
                return;
            }

            var innerLoopCounter = 0;

            foreach (var loopTracks in tracksList)
            {
                innerLoopCounter++;

                var newEntry = await LineGenerator.NewFromGpxTrack(loopTracks, false, statusContext.ProgressTracker());

                await ThreadSwitcher.ResumeForegroundAsync();

                var editor = new LineContentEditorWindow(newEntry);
                editor.PositionWindowAndShow();

                await ThreadSwitcher.ResumeBackgroundAsync();

                statusContext.Progress(
                    $"New Line Editor - {loopFile.FullName} - Track {innerLoopCounter} of {tracksList.Count}");
            }
        }
    }

    public async Task NewLinkContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = new LinkContentEditorWindow(null);

        newContentWindow.PositionWindowAndShow();
    }

    public async Task NewMapContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = new MapComponentEditorWindow(null);

        newContentWindow.PositionWindowAndShow();
    }


    public async Task NewNoteContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = new NoteContentEditorWindow(null);

        newContentWindow.PositionWindowAndShow();
    }


    public async Task NewPhotoContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = new PhotoContentEditorWindow();

        newContentWindow.PositionWindowAndShow();
    }

    public async Task NewPhotoContentFromFiles(bool autoSaveAndClose, CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting photo load.");

        var dialog = new VistaOpenFileDialog { Multiselect = true };

        if (!(dialog.ShowDialog() ?? false)) return;

        var selectedFiles = dialog.FileNames?.ToList() ?? new List<string>();

        if (!selectedFiles.Any()) return;

        if (!autoSaveAndClose && selectedFiles.Count > 10)
        {
            StatusContext.ToastError("Opening new content in an editor window is limited to 10 photos at a time...");
            return;
        }

        await ThreadSwitcher.ResumeBackgroundAsync();

        var selectedFileInfos = selectedFiles.Select(x => new FileInfo(x)).ToList();

        if (!selectedFileInfos.Any(x => x.Exists))
        {
            StatusContext.ToastError("Files don't exist?");
            return;
        }

        selectedFileInfos = selectedFileInfos.Where(x => x.Exists).ToList();

        if (!selectedFileInfos.Any(FileHelpers.PhotoFileTypeIsSupported))
        {
            StatusContext.ToastError("None of the files appear to be supported file types...");
            return;
        }

        if (selectedFileInfos.Any(x => !FileHelpers.PhotoFileTypeIsSupported(x)))
            StatusContext.ToastWarning(
                $"Skipping - not supported - {string.Join(", ", selectedFileInfos.Where(x => !FileHelpers.PhotoFileTypeIsSupported(x)))}");

        var validFiles = selectedFileInfos.Where(FileHelpers.PhotoFileTypeIsSupported).ToList();

        var loopCount = 0;

        foreach (var loopFile in validFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            loopCount++;

            WindowStatus?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.Normal, (decimal)loopCount / (validFiles.Count + 1)));

            await ThreadSwitcher.ResumeBackgroundAsync();

            if (autoSaveAndClose)
            {
                var (metaGenerationReturn, metaContent) = await
                    PhotoGenerator.PhotoMetadataToNewPhotoContent(loopFile, StatusContext.ProgressTracker());

                if (metaGenerationReturn.HasError)
                {
                    await ThreadSwitcher.ResumeForegroundAsync();

                    var editor = new PhotoContentEditorWindow(loopFile);
                    editor.PositionWindowAndShow();
#pragma warning disable 4014
                    //Allow execution to continue so Automation can continue
                    editor.StatusContext.ShowMessageWithOkButton("Problem Extracting Metadata",
                        metaGenerationReturn.GenerationNote);
#pragma warning restore 4014
                    continue;
                }

                var (saveGenerationReturn, _) = await PhotoGenerator.SaveAndGenerateHtml(metaContent, loopFile, true,
                    null, StatusContext.ProgressTracker());

                if (saveGenerationReturn.HasError)
                {
                    await ThreadSwitcher.ResumeForegroundAsync();

                    var editor = new PhotoContentEditorWindow(loopFile);
                    editor.PositionWindowAndShow();
#pragma warning disable 4014
                    //Allow execution to continue so Automation can continue
                    editor.StatusContext.ShowMessageWithOkButton("Problem Saving", saveGenerationReturn.GenerationNote);
#pragma warning restore 4014
                    continue;
                }
            }
            else
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                var editor = new PhotoContentEditorWindow(loopFile);
                editor.PositionWindowAndShow();
            }

            StatusContext.Progress($"New Photo Editor - {loopFile.FullName} ");

            await ThreadSwitcher.ResumeBackgroundAsync();
        }
    }

    public async Task NewPointContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = new PointContentEditorWindow(null);

        newContentWindow.PositionWindowAndShow();
    }

    public async Task NewPostContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = new PostContentEditorWindow();

        newContentWindow.PositionWindowAndShow();
    }
}