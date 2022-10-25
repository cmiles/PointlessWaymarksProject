using System.IO;
using System.Windows.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
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
using PointlessWaymarks.SpatialTools;
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
                    async () => await NewLineContentFromFiles(x, false, StatusContext, WindowStatus),
                    StatusContext.StatusControlContextId);
            }, "Cancel Line Import");
        NewLineContentFromFilesWithAutosaveCommand = StatusContext.RunBlockingTaskWithCancellationCommand(
            async x =>
            {
                await WindowIconStatus.IndeterminateTask(WindowStatus,
                    async () => await NewLineContentFromFiles(x, true, StatusContext, WindowStatus),
                    StatusContext.StatusControlContextId);
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

    public RelayCommand NewLineContentFromFilesWithAutosaveCommand { get; set; }

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
        var newContentWindow = await FileContentEditorWindow.CreateInstance();
        await newContentWindow.PositionWindowAndShowOnUiThread();
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

            var editor = await FileContentEditorWindow.CreateInstance(loopFile);
            await editor.PositionWindowAndShowOnUiThread();

            StatusContext.Progress($"New File Editor - {loopFile.FullName} ");

            await ThreadSwitcher.ResumeBackgroundAsync();
        }
    }

    public async Task NewGeoJsonContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = await GeoJsonContentEditorWindow.CreateInstance(null);

        newContentWindow.PositionWindowAndShow();
    }

    public async Task NewGpxImport()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = await GpxImportWindow.CreateInstance(null);

        newContentWindow.PositionWindowAndShow();
    }

    public async Task NewImageContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = await ImageContentEditorWindow.CreateInstance();

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

            var editor = await ImageContentEditorWindow.CreateInstance(initialImage: loopFile);
            editor.PositionWindowAndShow();

            StatusContext.Progress($"New Image Editor - {loopFile.FullName} ");

            await ThreadSwitcher.ResumeBackgroundAsync();
        }
    }

    public async Task NewLineContent()
    {
        var newContentWindow = await LineContentEditorWindow.CreateInstance(null);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    public static async Task NewLineContentFromFiles(CancellationToken cancellationToken, bool autoSaveAndClose,
        StatusControlContext statusContext, WindowIconStatus windowStatus)
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

        await NewLineContentFromFiles(selectedFileInfos, autoSaveAndClose, cancellationToken, statusContext,
            windowStatus);
    }

    public static async Task NewLineContentFromFiles(List<FileInfo> selectedFileInfos, bool autoSaveAndClose,
        CancellationToken cancellationToken,
        StatusControlContext statusContext, WindowIconStatus windowStatus)
    {
        var outerLoopCounter = 0;

        var skipFeatureIntersectionTagging = false;

        if (selectedFileInfos.Count > 10 && !string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile))
        {
            skipFeatureIntersectionTagging = await statusContext.ShowMessage("Slow Feature Intersection Tag Warning",
                $"You are importing {selectedFileInfos.Count} files, checking for Feature Intersection Tags on these will be slow, it will be faster to select all of the new entries in the Line List after they have been created/saved and generate Feature Intersection Tags then - skip Feature Intersection Tagging?",
                new List<string> { "Yes", "No" }) == "Yes";
        }

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

                var newEntry = await LineGenerator.NewFromGpxTrack(loopTracks, false, skipFeatureIntersectionTagging, statusContext.ProgressTracker());

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

    public async Task NewLinkContent()
    {
        var newContentWindow = await LinkContentEditorWindow.CreateInstance(null);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    public async Task NewMapContent()
    {
        var newContentWindow = await MapComponentEditorWindow.CreateInstance(null);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }


    public async Task NewNoteContent()
    {
        var newContentWindow = await NoteContentEditorWindow.CreateInstance(null);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }


    public async Task NewPhotoContent()
    {
        var newContentWindow = await PhotoContentEditorWindow.CreateInstance();

        await newContentWindow.PositionWindowAndShowOnUiThread();
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

        var intersectionTagger = new FeatureIntersectionTags.Intersection();

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
                    var editor = await PhotoContentEditorWindow.CreateInstance(loopFile);
                    await editor.PositionWindowAndShowOnUiThread();
#pragma warning disable 4014
                    //Allow execution to continue so Automation can continue
                    editor.StatusContext.ShowMessageWithOkButton("Problem Extracting Metadata",
                        metaGenerationReturn.GenerationNote);
#pragma warning restore 4014
                    continue;
                }

                if (metaContent.Latitude != null && metaContent.Longitude != null)
                {
                    var intersectionTags = intersectionTagger.Tags(
                        UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile,
                        new List<IFeature>()
                        {
                            new Feature(new Point(metaContent.Longitude.Value, metaContent.Latitude.Value),
                                new AttributesTable())
                        }, cancellationToken, StatusContext.ProgressTracker());

                    if (intersectionTags.Any())
                    {
                        var allTags = intersectionTags.SelectMany(x => x.Tags.Select(y => y).ToList());
                        var tagList = Db.TagListParse(metaContent.Tags).Union(allTags).ToList();
                        metaContent.Tags = Db.TagListJoin(tagList);
                    }
                }

                var (saveGenerationReturn, _) = await PhotoGenerator.SaveAndGenerateHtml(metaContent, loopFile, true,
                    null, StatusContext.ProgressTracker());

                if (saveGenerationReturn.HasError)
                {
                    var editor = await PhotoContentEditorWindow.CreateInstance(loopFile);
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

            StatusContext.Progress($"New Photo Editor - {loopFile.FullName} ");

            await ThreadSwitcher.ResumeBackgroundAsync();
        }
    }

    public async Task NewPointContent()
    {
        var newContentWindow = await PointContentEditorWindow.CreateInstance(null);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    public async Task NewPostContent()
    {
        var newContentWindow = await PostContentEditorWindow.CreateInstance();

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }
}