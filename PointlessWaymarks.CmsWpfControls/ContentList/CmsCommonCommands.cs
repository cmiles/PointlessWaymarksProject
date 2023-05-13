using System.IO;
using System.Windows.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
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
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

public partial class CmsCommonCommands : ObservableObject
{
    [ObservableProperty] private StatusControlContext _statusContext;

    public CmsCommonCommands(StatusControlContext? statusContext, WindowIconStatus? windowStatus = null)
    {
        _statusContext = statusContext ?? new StatusControlContext();
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
        NewVideoContentCommand = StatusContext.RunNonBlockingTaskCommand(NewVideoContent);
        NewVideoContentFromFilesCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(async x => await NewVideoContentFromFiles(x),
                "Cancel Video Import");

        NewGpxImportWindowCommand = StatusContext.RunNonBlockingTaskCommand(NewGpxImport);


        NewAllContentListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            var newWindow =
                await AllContentListWindow.CreateInstance(await AllContentListWithActionsContext.CreateInstance(null, WindowStatus));
            await newWindow.PositionWindowAndShowOnUiThread();
        });
        NewFileListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            var newWindow = await FileListWindow.CreateInstance(await FileListWithActionsContext.CreateInstance(null, WindowStatus));
            await newWindow.PositionWindowAndShowOnUiThread();
        });
        NewGeoJsonListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            var newWindow =
                await GeoJsonListWindow.CreateInstance(await GeoJsonListWithActionsContext.CreateInstance(null, WindowStatus));
            await newWindow.PositionWindowAndShowOnUiThread();
        });
        NewImageListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            var newWindow = await ImageListWindow.CreateInstance(await ImageListWithActionsContext.CreateInstance(null, WindowStatus));
            await newWindow.PositionWindowAndShowOnUiThread();
        });
        NewLineListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            var newWindow = await LineListWindow.CreateInstance(await LineListWithActionsContext.CreateInstance(null, WindowStatus));
            await newWindow.PositionWindowAndShowOnUiThread();
        });
        NewLinkListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            var newWindow = await LinkListWindow.CreateInstance(await LinkListWithActionsContext.CreateInstance(null, WindowStatus));
            await newWindow.PositionWindowAndShowOnUiThread();
        });
        NewMapComponentListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            var newWindow =
                await MapComponentListWindow.CreateInstance(await MapComponentListWithActionsContext.CreateInstance(null, WindowStatus));
            await newWindow.PositionWindowAndShowOnUiThread();
        });
        NewNoteListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            var newWindow = await NoteListWindow.CreateInstance(await NoteListWithActionsContext.CreateInstance(null, WindowStatus));
            await newWindow.PositionWindowAndShowOnUiThread();
        });
        NewPhotoListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            var newWindow = await PhotoListWindow.CreateInstance(await PhotoListWithActionsContext.CreateInstance(null, WindowStatus, null));
            await newWindow.PositionWindowAndShowOnUiThread();
        });
        NewPointListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            var newWindow = await PointListWindow.CreateInstance(await PointListWithActionsContext.CreateInstance(null, WindowStatus));
            await newWindow.PositionWindowAndShowOnUiThread();
        });
        NewPostListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            var newWindow = await PostListWindow.CreateInstance(await PostListWithActionsContext.CreateInstance(null, WindowStatus));
            await newWindow.PositionWindowAndShowOnUiThread();
        });
        NewVideoListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            var newWindow = await VideoListWindow.CreateInstance(await VideoListWithActionsContext.CreateInstance(null, WindowStatus));
            await newWindow.PositionWindowAndShowOnUiThread();
        });

        SearchHelpWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            var newWindow = await MarkdownViewerWindow.CreateInstance("Search Help", SearchHelpMarkdown.HelpBlock);
            await newWindow.PositionWindowAndShowOnUiThread();
        });

        GenerateChangedHtmlAndStartUploadCommand =
            StatusContext.RunBlockingTaskCommand(GenerateChangedHtmlAndStartUpload);
        GenerateChangedHtmlCommand = StatusContext.RunBlockingTaskCommand(GenerateChangedHtml);
        ShowSitePreviewWindowCommand = StatusContext.RunNonBlockingTaskCommand(ShowSitePreviewWindow);
        GenerateChangedHtmlAndShowSitePreviewCommand =
            StatusContext.RunBlockingTaskCommand(GenerateChangedHtmlAndShowSitePreview);
    }

    public RelayCommand GenerateChangedHtmlAndShowSitePreviewCommand { get; }

    public RelayCommand GenerateChangedHtmlAndStartUploadCommand { get; }

    public RelayCommand GenerateChangedHtmlCommand { get; }

    public RelayCommand NewAllContentListWindowCommand { get; }

    public RelayCommand NewFileContentCommand { get; }

    public RelayCommand NewFileContentFromFilesCommand { get; }

    public RelayCommand NewFileListWindowCommand { get; }

    public RelayCommand NewGeoJsonContentCommand { get; }

    public RelayCommand NewGeoJsonListWindowCommand { get; }

    public RelayCommand NewGpxImportWindowCommand { get; }

    public RelayCommand NewImageContentCommand { get; }

    public RelayCommand NewImageContentFromFilesCommand { get; }

    public RelayCommand NewImageListWindowCommand { get; }

    public RelayCommand NewLineContentCommand { get; }

    public RelayCommand NewLineContentFromFilesCommand { get; }

    public RelayCommand NewLineContentFromFilesWithAutosaveCommand { get; }

    public RelayCommand NewLineListWindowCommand { get; }

    public RelayCommand NewLinkContentCommand { get; }

    public RelayCommand NewLinkListWindowCommand { get; }

    public RelayCommand NewMapComponentListWindowCommand { get; }

    public RelayCommand NewMapContentCommand { get; }

    public RelayCommand NewNoteContentCommand { get; }

    public RelayCommand NewNoteListWindowCommand { get; }

    public RelayCommand NewPhotoContentCommand { get; }

    public RelayCommand NewPhotoContentFromFilesCommand { get; }

    public RelayCommand NewPhotoContentFromFilesWithAutosaveCommand { get; }

    public RelayCommand NewPhotoListWindowCommand { get; set; }

    public RelayCommand NewPointContentCommand { get; }

    public RelayCommand NewPointListWindowCommand { get; }

    public RelayCommand NewPostContentCommand { get; }

    public RelayCommand NewPostListWindowCommand { get; }

    public RelayCommand NewVideoContentCommand { get; }

    public RelayCommand NewVideoContentFromFilesCommand { get; }

    public RelayCommand NewVideoListWindowCommand { get; }

    public RelayCommand SearchHelpWindowCommand { get; }

    public RelayCommand ShowSitePreviewWindowCommand { get; }

    public WindowIconStatus? WindowStatus { get; }

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

    public async Task NewLineContent()
    {
        var newContentWindow = await LineContentEditorWindow.CreateInstance(null);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    public static async Task NewLineContentFromFiles(CancellationToken cancellationToken, bool autoSaveAndClose,
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

        await NewLineContentFromFiles(selectedFileInfos, autoSaveAndClose, cancellationToken, statusContext,
            windowStatus);
    }

    public static async Task NewLineContentFromFiles(List<FileInfo> selectedFileInfos, bool autoSaveAndClose,
        CancellationToken cancellationToken,
        StatusControlContext statusContext, WindowIconStatus? windowStatus)
    {
        var outerLoopCounter = 0;

        var skipFeatureIntersectionTagging = false;

        if (selectedFileInfos.Count > 10 &&
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
                    statusContext.ProgressTracker());

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
                var (metaGenerationReturn, metaContent) = await
                    PhotoGenerator.PhotoMetadataToNewPhotoContent(loopFile, StatusContext.ProgressTracker());

                if (metaGenerationReturn.HasError || metaContent == null)
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

                if (metaContent is { Latitude: { }, Longitude: { } })
                {
                    var photoPointFeature = new Feature(
                        new Point(metaContent.Longitude.Value, metaContent.Latitude.Value),
                        new AttributesTable());

                    var intersectionTags = photoPointFeature.IntersectionTags(
                        UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile, cancellationToken,
                        StatusContext.ProgressTracker());

                    if (intersectionTags.Any())
                    {
                        var tagList = Db.TagListParse(metaContent.Tags).Union(intersectionTags).ToList();
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

    public async Task NewVideoContent()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = await VideoContentEditorWindow.CreateInstance();

        newContentWindow.PositionWindowAndShow();
    }

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

    private async Task ShowSitePreviewWindow()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var sitePreviewWindow = await SiteOnDiskPreviewWindow.CreateInstance();

        await sitePreviewWindow.PositionWindowAndShowOnUiThread();
    }
}