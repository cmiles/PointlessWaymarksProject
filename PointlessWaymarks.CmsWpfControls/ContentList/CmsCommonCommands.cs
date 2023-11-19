using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Shell;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsWpfControls.AllContentList;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.CmsWpfControls.GeoJsonContentEditor;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.GpxImport;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.ImageContentEditor;
using PointlessWaymarks.CmsWpfControls.ImageList;
using PointlessWaymarks.CmsWpfControls.LineContentEditor;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.CmsWpfControls.LinkList;
using PointlessWaymarks.CmsWpfControls.MapComponentEditor;
using PointlessWaymarks.CmsWpfControls.MapComponentList;
using PointlessWaymarks.CmsWpfControls.MarkdownViewer;
using PointlessWaymarks.CmsWpfControls.NoteContentEditor;
using PointlessWaymarks.CmsWpfControls.NoteList;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CmsWpfControls.PostContentEditor;
using PointlessWaymarks.CmsWpfControls.PostList;
using PointlessWaymarks.CmsWpfControls.S3Uploads;
using PointlessWaymarks.CmsWpfControls.SitePreview;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.VideoContentEditor;
using PointlessWaymarks.CmsWpfControls.VideoList;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class CmsCommonCommands
{
    public CmsCommonCommands(StatusControlContext? statusContext, WindowIconStatus? windowStatus = null)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;

        BuildCommands();
    }

    public StatusControlContext StatusContext { get; set; }

    public WindowIconStatus? WindowStatus { get; }

    [BlockingCommand]
    private async Task GenerateChangedHtml()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        try
        {
            WindowStatus?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.Indeterminate));

            await HtmlGenerationGroups.GenerateChangedToHtml(StatusContext.ProgressTracker());
        }
        finally
        {
            WindowStatus?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.None));
        }
    }

    [BlockingCommand]
    private async Task GenerateChangedHtmlAndShowSitePreview()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        try
        {
            WindowStatus?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.Indeterminate));

            await HtmlGenerationGroups.GenerateChangedToHtml(StatusContext.ProgressTracker());
        }
        finally
        {
            WindowStatus?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.None));
        }

        var sitePreviewWindow = await SiteOnDiskPreviewWindow.CreateInstance();
        await sitePreviewWindow.PositionWindowAndShowOnUiThread();
    }

    [BlockingCommand]
    private async Task GenerateChangedHtmlAndStartUpload()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        try
        {
            WindowStatus?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.Indeterminate));

            await S3UploadHelpers.GenerateChangedHtmlAndStartUpload(StatusContext, WindowStatus);
        }
        finally
        {
            WindowStatus?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.None));
        }
    }


    [NonBlockingCommand]
    private async Task NewAllContentListWindow()
    {
        var newWindow =
            await AllContentListWindow.CreateInstance(
                await AllContentListWithActionsContext.CreateInstance(null, WindowStatus));
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    private async Task NewCmsWindow()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        //6/18/2023 - Assembly.GetEntryAssembly()!.Location might be more expected but I saw some indication
        //in the online material that the method below might be more durable. The dll/exe swap is a bit of 
        //a guess but at the least seems to help inside Visual Studio...
        var command = Environment.GetCommandLineArgs()[0];
        if (command.EndsWith(".dll")) command = $"{command[..^4]}.exe";

        Process.Start(command);
    }

    [NonBlockingCommand]
    public async Task NewFileContent()
    {
        var newContentWindow = await FileContentEditorWindow.CreateInstance();
        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    [BlockingCommand]
    private async Task NewFileContentFromFiles(CancellationToken cancellationToken)
    {
        await WindowIconStatus.IndeterminateTask(WindowStatus,
            async () => await NewFileContentFromFilesBase(cancellationToken),
            StatusContext.StatusControlContextId);
    }

    public async Task NewFileContentFromFilesBase(CancellationToken cancellationToken)
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

            var editor = await FileContentEditorWindow.CreateInstance(loopFile);
            await editor.PositionWindowAndShowOnUiThread();

            StatusContext.Progress($"New File Editor - {loopFile.FullName} ");

            await ThreadSwitcher.ResumeBackgroundAsync();
        }
    }

    [NonBlockingCommand]
    private async Task NewFileListWindow()
    {
        var newWindow =
            await FileListWindow.CreateInstance(
                await FileListWithActionsContext.CreateInstance(null, WindowStatus));
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    public async Task NewGeoJsonContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = await GeoJsonContentEditorWindow.CreateInstance(null);

        newContentWindow.PositionWindowAndShow();
    }

    [NonBlockingCommand]
    private async Task NewGeoJsonListWindow()
    {
        var newWindow =
            await GeoJsonListWindow.CreateInstance(
                await GeoJsonListWithActionsContext.CreateInstance(null, WindowStatus));
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    public async Task NewGpxImportWindow()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = await GpxImportWindow.CreateInstance(null);

        newContentWindow.PositionWindowAndShow();
    }

    [NonBlockingCommand]
    public async Task NewImageContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = await ImageContentEditorWindow.CreateInstance();

        newContentWindow.PositionWindowAndShow();
    }

    [BlockingCommand]
    public async Task NewImageContentFromFiles(CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting Image load.");

        var dialog = new VistaOpenFileDialog { Multiselect = true, Filter = "jpg files (*.jpg;*.jpeg)|*.jpg;*.jpeg" };

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

            var editor = await ImageContentEditorWindow.CreateInstance(initialImage: loopFile);
            editor.PositionWindowAndShow();

            StatusContext.Progress($"New Image Editor - {loopFile.FullName} ");

            await ThreadSwitcher.ResumeBackgroundAsync();
        }
    }


    [NonBlockingCommand]
    private async Task NewImageListWindow()
    {
        var newWindow =
            await ImageListWindow.CreateInstance(
                await ImageListWithActionsContext.CreateInstance(null, WindowStatus));
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    public async Task NewLineContent()
    {
        var newContentWindow = await LineContentEditorWindow.CreateInstance(null);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    [BlockingCommand]
    private async Task NewLineContentFromFiles(CancellationToken cancellationToken)
    {
        await WindowIconStatus.IndeterminateTask(WindowStatus,
            async () => await NewLineContentFromFilesBase(cancellationToken, false, false, StatusContext, WindowStatus),
            StatusContext.StatusControlContextId);
    }

    public static async Task NewLineContentFromFilesBase(CancellationToken cancellationToken,
        bool addTimeAssociatedPhotosToBody, bool autoSaveAndClose,
        StatusControlContext statusContext, WindowIconStatus? windowStatus)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        statusContext.Progress("Starting Line load.");

        var dialog = new VistaOpenFileDialog { Multiselect = true };

        if (!(dialog.ShowDialog() ?? false)) return;

        var selectedFiles = dialog.FileNames?.ToList() ?? new List<string>();

        if (!selectedFiles.Any()) return;

        if (!autoSaveAndClose && selectedFiles.Count > 10)
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

        await NewLineContentFromFilesBase(selectedFileInfos, addTimeAssociatedPhotosToBody, autoSaveAndClose,
            cancellationToken, statusContext,
            windowStatus);
    }

    public static async Task NewLineContentFromFilesBase(List<FileInfo> selectedFileInfos,
        bool addTimeAssociatedPhotosToBody, bool autoSaveAndClose,
        CancellationToken cancellationToken,
        StatusControlContext statusContext, WindowIconStatus? windowStatus)
    {
        var outerLoopCounter = 0;

        var skipFeatureIntersectionTagging = false;

        if (selectedFileInfos.Count > 10 && UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagOnImport &&
            !string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile))
            skipFeatureIntersectionTagging = await statusContext.ShowMessage("Slow Feature Intersection Tag Warning",
                $"You are importing {selectedFileInfos.Count} files, checking for Feature Intersection Tags on these will be slow, it will be faster to select all of the new entries in the Line List after they have been created/saved and generate Feature Intersection Tags then - skip Feature Intersection Tagging?",
                new List<string> { "Yes", "No" }) == "Yes";

        foreach (var loopFile in selectedFileInfos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            outerLoopCounter++;

            windowStatus?.AddRequest(new WindowIconStatusRequest(statusContext.StatusControlContextId,
                TaskbarItemProgressState.Normal, (decimal)outerLoopCounter / (selectedFileInfos.Count + 1)));

            var tracksList = await GpxTools.TracksFromGpxFile(loopFile, statusContext.ProgressTracker());

            if (tracksList.Count < 1 || tracksList.All(x => x.Track.Count < 2))
            {
                statusContext.ToastWarning($"No Tracks in {loopFile.Name}? Skipping...");
                continue;
            }

            var innerLoopCounter = 0;

            foreach (var loopTracks in tracksList.Where(x => x.Track.Count > 1))
            {
                innerLoopCounter++;

                var newEntry = await LineGenerator.NewFromGpxTrack(loopTracks, false, skipFeatureIntersectionTagging,
                    addTimeAssociatedPhotosToBody, statusContext.ProgressTracker());

                if (autoSaveAndClose)
                {
                    var (saveGenerationReturn, _) =
                        await LineGenerator.SaveAndGenerateHtml(newEntry, DateTime.Now,
                            statusContext.ProgressTracker());

                    if (saveGenerationReturn.HasError)
                    {
                        var editor = await LineContentEditorWindow.CreateInstance(newEntry);
                        await editor.PositionWindowAndShowOnUiThread();
#pragma warning disable 4014
                        //Allow execution to continue so Automation can continue
                        editor.StatusContext.ShowMessageWithOkButton("Problem Saving",
                            saveGenerationReturn.GenerationNote);
#pragma warning restore 4014
                        continue;
                    }
                }
                else
                {
                    var editor = await LineContentEditorWindow.CreateInstance(newEntry);
                    await editor.PositionWindowAndShowOnUiThread();
                }

                statusContext.Progress(
                    $"New Line Editor - {loopFile.FullName} - Track {innerLoopCounter} of {tracksList.Count}");
            }
        }
    }

    [BlockingCommand]
    private async Task NewLineContentFromFilesWithAutosave(CancellationToken cancellationToken)
    {
        await WindowIconStatus.IndeterminateTask(WindowStatus,
            async () => await NewLineContentFromFilesBase(cancellationToken, false, true, StatusContext, WindowStatus),
            StatusContext.StatusControlContextId);
    }

    [BlockingCommand]
    private async Task NewLineContentFromFilesWithPhotos(CancellationToken cancellationToken)
    {
        await WindowIconStatus.IndeterminateTask(WindowStatus,
            async () => await NewLineContentFromFilesBase(cancellationToken, true, false, StatusContext, WindowStatus),
            StatusContext.StatusControlContextId);
    }

    [BlockingCommand]
    private async Task NewLineContentFromFilesWithPhotosWithAutosave(CancellationToken cancellationToken)
    {
        await WindowIconStatus.IndeterminateTask(WindowStatus,
            async () => await NewLineContentFromFilesBase(cancellationToken, true, true, StatusContext, WindowStatus),
            StatusContext.StatusControlContextId);
    }

    [NonBlockingCommand]
    private async Task NewLineListWindow()
    {
        var newWindow =
            await LineListWindow.CreateInstance(
                await LineListWithActionsContext.CreateInstance(null, WindowStatus));
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    public async Task NewLinkContent()
    {
        var newContentWindow = await LinkContentEditorWindow.CreateInstance(null);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }


    [NonBlockingCommand]
    private async Task NewLinkListWindow()
    {
        var newWindow =
            await LinkListWindow.CreateInstance(
                await LinkListWithActionsContext.CreateInstance(null, WindowStatus));
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    private async Task NewMapComponentListWindow()
    {
        var newWindow =
            await MapComponentListWindow.CreateInstance(
                await MapComponentListWithActionsContext.CreateInstance(null, WindowStatus));
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    public async Task NewMapContent()
    {
        var newContentWindow = await MapComponentEditorWindow.CreateInstance(null);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    public async Task NewNoteContent()
    {
        var newContentWindow = await NoteContentEditorWindow.CreateInstance(null);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    private async Task NewNoteListWindow()
    {
        var newWindow =
            await NoteListWindow.CreateInstance(
                await NoteListWithActionsContext.CreateInstance(null, WindowStatus));
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    public async Task NewPhotoContent()
    {
        var newContentWindow = await PhotoContentEditorWindow.CreateInstance();

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    [BlockingCommand]
    private async Task NewPhotoContentFromFiles(CancellationToken cancellationToken)
    {
        await WindowIconStatus.IndeterminateTask(WindowStatus,
            async () => await NewPhotoContentFromFilesBase(false, false, cancellationToken),
            StatusContext.StatusControlContextId);
    }

    public async Task NewPhotoContentFromFilesBase(bool autoSaveAndClose, bool adjustFilename, CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting photo load.");

        var dialog = new VistaOpenFileDialog { Multiselect = true, Filter = "jpg files (*.jpg;*.jpeg)|*.jpg;*.jpeg" };

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
                var photoFile = loopFile;

                var (metaGenerationReturn, metaContent) = await
                    PhotoGenerator.PhotoMetadataToNewPhotoContent(photoFile, StatusContext.ProgressTracker());
                
                var fileNameValidation = await CommonContentValidation.PhotoFileValidation(photoFile, null);

                if (metaContent != null && (!fileNameValidation.Valid || adjustFilename))
                {
                    var newBaseName = adjustFilename && !string.IsNullOrWhiteSpace(metaContent.Title) ? metaContent.Title : Path.GetFileNameWithoutExtension(photoFile.Name);
                    var renameResult = await FileAndFolderTools.TryAutoRenameFileForProgramConventions(photoFile, newBaseName);

                    if (renameResult is { Exists: true })
                    {
                        metaContent.OriginalFileName = renameResult.FullName;
                        photoFile = renameResult;

                        (metaGenerationReturn, metaContent) = await
                            PhotoGenerator.PhotoMetadataToNewPhotoContent(photoFile, StatusContext.ProgressTracker());
                    }
                }

                if (metaGenerationReturn.HasError || metaContent == null)
                {
                    var editor = await PhotoContentEditorWindow.CreateInstance(photoFile);
                    await editor.PositionWindowAndShowOnUiThread();
#pragma warning disable 4014
                    //Allow execution to continue so Automation can continue
                    editor.StatusContext.ShowMessageWithOkButton("Problem Extracting Metadata",
                        metaGenerationReturn.GenerationNote);
#pragma warning restore 4014
                    continue;
                }

                var (saveGenerationReturn, _) = await PhotoGenerator.SaveAndGenerateHtml(metaContent, photoFile, true,
                    null, StatusContext.ProgressTracker());

                if (saveGenerationReturn.HasError)
                {
                    var editor = await PhotoContentEditorWindow.CreateInstance(photoFile);
                    await editor.PositionWindowAndShowOnUiThread();
#pragma warning disable 4014
                    //Allow execution to continue so Automation can continue
                    editor.StatusContext.ShowMessageWithOkButton("Problem Saving", saveGenerationReturn.GenerationNote);
#pragma warning restore 4014
                    continue;
                }
            }
            else
            {
                var editor = await PhotoContentEditorWindow.CreateInstance(loopFile);
                await editor.PositionWindowAndShowOnUiThread();
            }

            StatusContext.Progress($"New Photo Editor - based on {loopFile.FullName} ");

            await ThreadSwitcher.ResumeBackgroundAsync();
        }
    }

    [BlockingCommand]
    private async Task NewPhotoContentFromFilesWithAutosave(CancellationToken cancellationToken)
    {
        await WindowIconStatus.IndeterminateTask(WindowStatus,
            async () => await NewPhotoContentFromFilesBase(true, true, cancellationToken),
            StatusContext.StatusControlContextId);
    }


    [NonBlockingCommand]
    private async Task NewPhotoListWindow()
    {
        var newWindow =
            await PhotoListWindow.CreateInstance(
                await PhotoListWithActionsContext.CreateInstance(null, WindowStatus, null));
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    public async Task NewPointContent()
    {
        var newContentWindow = await PointContentEditorWindow.CreateInstance(null);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    private async Task NewPointListWindow()
    {
        var newWindow =
            await PointListWindow.CreateInstance(
                await PointListWithActionsContext.CreateInstance(null, WindowStatus));
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    public async Task NewPostContent()
    {
        var newContentWindow = await PostContentEditorWindow.CreateInstance();

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    private async Task NewPostList()
    {
        var newWindow =
            await PostListWindow.CreateInstance(
                await PostListWithActionsContext.CreateInstance(null, WindowStatus));
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    public async Task NewVideoContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = await VideoContentEditorWindow.CreateInstance();

        newContentWindow.PositionWindowAndShow();
    }

    [BlockingCommand]
    public async Task NewVideoContentFromFiles(CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting Video load.");

        var dialog = new VistaOpenFileDialog
            { Multiselect = true, Filter = "supported formats (*.mp4;*.webm,*.ogg)|*.mp4;*.webm;*.ogg" };

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

        if (!selectedFileInfos.Any(FileHelpers.VideoFileTypeIsSupported))
        {
            StatusContext.ToastError("None of the files appear to be supported file types...");
            return;
        }

        if (selectedFileInfos.Any(x => !FileHelpers.VideoFileTypeIsSupported(x)))
            StatusContext.ToastWarning(
                $"Skipping - not supported - {string.Join(", ", selectedFileInfos.Where(x => !FileHelpers.VideoFileTypeIsSupported(x)))}");

        foreach (var loopFile in selectedFileInfos.Where(FileHelpers.VideoFileTypeIsSupported))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ThreadSwitcher.ResumeForegroundAsync();

            var editor = await VideoContentEditorWindow.CreateInstance(loopFile);
            editor.PositionWindowAndShow();

            StatusContext.Progress($"New Video Editor - {loopFile.FullName} ");

            await ThreadSwitcher.ResumeBackgroundAsync();
        }
    }

    [NonBlockingCommand]
    private async Task NewVideoListWindow()
    {
        var newWindow =
            await VideoListWindow.CreateInstance(
                await VideoListWithActionsContext.CreateInstance(null, WindowStatus));
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    private async Task SearchHelpWindow()
    {
        var newWindow = await MarkdownViewerWindow.CreateInstance("Search Help", SearchHelpMarkdown.HelpBlock);
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    private async Task ShowSitePreviewWindow()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var sitePreviewWindow = await SiteOnDiskPreviewWindow.CreateInstance();

        await sitePreviewWindow.PositionWindowAndShowOnUiThread();
    }
}