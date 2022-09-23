#nullable enable
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.S3Deletions;
using PointlessWaymarks.CmsWpfControls.S3Uploads;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.Aws;
using PointlessWaymarks.CmsWpfControls.Utility.Excel;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.FilesWrittenLogList;

[ObservableObject]
public partial class FilesWrittenLogListContext
{
    [ObservableProperty] private RelayCommand? _allFilesToExcelCommand;
    [ObservableProperty] private RelayCommand? _allScriptStringsToClipboardCommand;
    [ObservableProperty] private RelayCommand? _allScriptStringsToPowerShellScriptCommand;
    [ObservableProperty] private RelayCommand? _allWrittenFilesToClipboardCommand;
    [ObservableProperty] private RelayCommand _allWrittenFilesToRunningS3UploaderCommand;
    [ObservableProperty] private RelayCommand? _allWrittenFilesToS3UploaderCommand;
    [ObservableProperty] private RelayCommand? _allWrittenFilesToS3UploaderJsonFileCommand;
    [ObservableProperty] private bool _changeSlashes = true;
    [ObservableProperty] private DataNotificationsWorkQueue _dataNotificationsProcessor;
    [ObservableProperty] private bool _filterForFilesInCurrentGenerationDirectory = true;
    [ObservableProperty] private RelayCommand? _generateItemsCommand;
    [ObservableProperty] private ObservableCollection<FileWrittenLogListDateTimeFilterChoice>? _generationChoices;
    [ObservableProperty] private ObservableCollection<FilesWrittenLogListListItem>? _items;
    [ObservableProperty] private RelayCommand? _openUploaderJsonFileCommand;
    [ObservableProperty] private RelayCommand? _selectedFilesToExcelCommand;
    [ObservableProperty] private FileWrittenLogListDateTimeFilterChoice? _selectedGenerationChoice;
    [ObservableProperty] private List<FilesWrittenLogListListItem> _selectedItems = new();
    [ObservableProperty] private RelayCommand? _selectedScriptStringsToClipboardCommand;
    [ObservableProperty] private RelayCommand? _selectedScriptStringsToPowerShellScriptCommand;
    [ObservableProperty] private RelayCommand? _selectedWrittenFilesToClipboardCommand;
    [ObservableProperty] private RelayCommand _selectedWrittenFilesToRunningS3UploaderCommand;
    [ObservableProperty] private RelayCommand? _selectedWrittenFilesToS3UploaderCommand;
    [ObservableProperty] private RelayCommand? _selectedWrittenFilesToS3UploaderJsonFileCommand;
    [ObservableProperty] private RelayCommand? _siteDeletedFilesReportCommand;
    [ObservableProperty] private RelayCommand? _siteMissingFilesReportCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _userBucketName = string.Empty;
    [ObservableProperty] private string _userBucketRegion = string.Empty;
    [ObservableProperty] private string _userScriptPrefix = "aws s3 cp";

    public FilesWrittenLogListContext(StatusControlContext? statusContext, bool loadInBackground)
    {
        _statusContext = statusContext ?? new StatusControlContext();

        GenerateItemsCommand = StatusContext.RunBlockingTaskCommand(GenerateItems);
        AllScriptStringsToClipboardCommand = StatusContext.RunBlockingTaskCommand(AllScriptStringsToClipboard);
        SelectedScriptStringsToClipboardCommand =
            StatusContext.RunBlockingTaskCommand(SelectedScriptStringsToClipboard);
        AllWrittenFilesToClipboardCommand = StatusContext.RunBlockingTaskCommand(AllWrittenFilesToClipboard);
        SelectedWrittenFilesToClipboardCommand = StatusContext.RunBlockingTaskCommand(SelectedWrittenFilesToClipboard);
        SelectedScriptStringsToPowerShellScriptCommand =
            StatusContext.RunBlockingTaskCommand(SelectedScriptStringsToPowerShellScript);
        AllScriptStringsToPowerShellScriptCommand =
            StatusContext.RunBlockingTaskCommand(AllScriptStringsToPowerShellScript);
        SelectedFilesToExcelCommand = StatusContext.RunBlockingTaskCommand(SelectedFilesToExcel);
        AllFilesToExcelCommand = StatusContext.RunBlockingTaskCommand(AllFilesToExcel);
        SiteMissingFilesReportCommand = StatusContext.RunBlockingTaskCommand(SiteMissingAndChangedFilesReport);
        SiteDeletedFilesReportCommand = StatusContext.RunBlockingTaskCommand(SiteDeletedFilesReport);
        SelectedWrittenFilesToS3UploaderCommand =
            StatusContext.RunNonBlockingTaskCommand(async () => await SelectedWrittenFilesToS3Uploader(false));
        AllWrittenFilesToS3UploaderCommand =
            StatusContext.RunNonBlockingTaskCommand(async () => await AllWrittenFilesToS3Uploader(false));
        SelectedWrittenFilesToRunningS3UploaderCommand =
            StatusContext.RunNonBlockingTaskCommand(async () => await SelectedWrittenFilesToS3Uploader(true));
        AllWrittenFilesToRunningS3UploaderCommand =
            StatusContext.RunNonBlockingTaskCommand(async () => await AllWrittenFilesToS3Uploader(true));
        SelectedWrittenFilesToS3UploaderJsonFileCommand =
            StatusContext.RunNonBlockingTaskCommand(SelectedWrittenFilesToS3UploaderJsonFile);
        AllWrittenFilesToS3UploaderJsonFileCommand =
            StatusContext.RunNonBlockingTaskCommand(AllWrittenFilesToS3UploaderJsonFile);
        OpenUploaderJsonFileCommand = StatusContext.RunNonBlockingTaskCommand(OpenUploaderJsonFile);

        PropertyChanged += OnPropertyChanged;

        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(LoadData);

        _dataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    private async Task AllFilesToExcel()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (Items == null || !Items.Any())
        {
            StatusContext.ToastError("No Items?");
            return;
        }

        FilesToExcel(Items.ToList());
    }

    public async Task AllScriptStringsToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (Items == null || !Items.Any())
        {
            StatusContext.ToastError("No Items?");
            return;
        }

        var scriptString = FileItemsToScriptString(Items.ToList());

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(scriptString);

        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.ToastSuccess("Items added to Clipboard");
    }

    public async Task AllScriptStringsToPowerShellScript()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (Items == null || !Items.Any())
        {
            StatusContext.ToastError("No Items Selected?");
            return;
        }

        await FileItemsToScriptFile(Items.ToList()).ConfigureAwait(true);
    }

    public async Task AllWrittenFilesToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (Items == null || !Items.Any())
        {
            StatusContext.ToastError("No Items?");
            return;
        }

        await FilesToClipboard(Items.ToList()).ConfigureAwait(true);
    }

    public async Task AllWrittenFilesToS3Uploader(bool autoStartUploader)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (Items == null || !Items.Any())
        {
            StatusContext.ToastError("No Items?");
            return;
        }

        await FileItemsToS3Uploader(Items.ToList(), autoStartUploader);
    }

    public async Task AllWrittenFilesToS3UploaderJsonFile()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (Items == null || !Items.Any())
        {
            StatusContext.ToastError("No Items?");
            return;
        }

        await FileItemsToS3UploaderJsonFile(Items.ToList());
    }

    public static async Task<FilesWrittenLogListContext> CreateInstance(StatusControlContext? statusContext)
    {
        var newContext = new FilesWrittenLogListContext(statusContext, false);
        await newContext.LoadData();
        return newContext;
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs? e)
    {
        var translatedMessage = DataNotifications.TranslateDataNotification(e?.Message);

        if (translatedMessage.HasError)
        {
            Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}",
                translatedMessage.ErrorNote, StatusContext.StatusControlContextId);
            return;
        }

        if (translatedMessage.ContentType is DataNotificationContentType.FileTransferScriptLog or DataNotificationContentType.GenerationLog)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            await LoadDateTimeFilterChoices();
        }
    }

    private async Task FileItemsToS3Uploader(List<FilesWrittenLogListListItem> items, bool autoStartUpload)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!items.Any()) return;

        var toTransfer = FileItemsToUploaderItems(items);

        if (!toTransfer.Any())
        {
            StatusContext.ToastError("No Files in the Generation Directory?");
            return;
        }

        var toSkipCount = SelectedItems.Count(x => !x.IsInGenerationDirectory);

        if (toSkipCount > 0)
            StatusContext.ToastWarning($"{toSkipCount} skipped files not in the Generation Directory");

        var fileName = Path.Combine(UserSettingsSingleton.CurrentSettings().LocalScriptsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---File-Upload-Data.json");

        await S3UploadHelpers.S3UploaderItemsToS3UploaderJsonFile(toTransfer, fileName);

        await ThreadSwitcher.ResumeForegroundAsync();

        var newUploadWindow = new S3UploadsWindow(toTransfer, autoStartUpload);
        newUploadWindow.PositionWindowAndShow();
    }

    private async Task FileItemsToS3UploaderJsonFile(List<FilesWrittenLogListListItem> items)
    {
        if (!items.Any()) return;

        var toTransfer = FileItemsToUploaderItems(items);

        var fileName = Path.Combine(UserSettingsSingleton.CurrentSettings().LocalScriptsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---File-Upload-Data.json");

        await S3UploadHelpers.S3UploaderItemsToS3UploaderJsonFile(toTransfer, fileName);

        await ProcessHelpers.OpenExplorerWindowForFile(fileName).ConfigureAwait(false);
    }

    public async Task FileItemsToScriptFile(List<FilesWrittenLogListListItem> toWrite)
    {
        var scriptString = FileItemsToScriptString(toWrite);

        var file = new FileInfo(Path.Combine(UserSettingsSingleton.CurrentSettings().LocalScriptsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---File-Upload-Script.ps1"));

        await File.WriteAllTextAsync(file.FullName, scriptString);

        await Db.SaveGenerationFileTransferScriptLog(new GenerationFileTransferScriptLog
        {
            FileName = file.FullName, WrittenOnVersion = DateTime.Now.TrimDateTimeToSeconds().ToUniversalTime()
        });

        await ProcessHelpers.OpenExplorerWindowForFile(file.FullName).ConfigureAwait(false);
    }

    public string FileItemsToScriptString(List<FilesWrittenLogListListItem> toConvert)
    {
        var sortedItems = toConvert.OrderByDescending(x => x.WrittenFile.Count(y => y == '\\'))
            .ThenBy(x => x.WrittenFile).ToList();

        sortedItems = sortedItems.Where(x => File.Exists(x.WrittenFile)).ToList();

        return string.Join(Environment.NewLine,
            sortedItems.Where(x => x.IsInGenerationDirectory).Select(x =>
                    $"{UserScriptPrefix}{(string.IsNullOrWhiteSpace(UserScriptPrefix) ? "" : " ")}'{x.WrittenFile}' s3://{x.TransformedFile};")
                .Distinct().ToList());
    }


    private List<S3Upload> FileItemsToUploaderItems(List<FilesWrittenLogListListItem> items)
    {
        return items.Where(x => x.IsInGenerationDirectory && File.Exists(x.WrittenFile)).Select(x =>
            new S3Upload(new FileInfo(x.WrittenFile),
                AwsS3GeneratedSiteComparisonForAdditionsAndChanges.FileInfoInGeneratedSiteToS3Key(
                    new FileInfo(x.WrittenFile)), UserBucketName, UserBucketRegion,
                $"From Files Written Log - {x.WrittenOn}")).ToList();
    }

    private async Task FilesToClipboard(List<FilesWrittenLogListListItem> items)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var scriptString = string.Join(Environment.NewLine, items.Select(x => x.WrittenFile).Distinct().ToList());

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(scriptString);

        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.ToastSuccess("Items added to Clipboard");
    }

    private void FilesToExcel(List<FilesWrittenLogListListItem> items)
    {
        ExcelHelpers.ContentToExcelFileAsTable(items.Cast<object>().ToList(), "WrittenFiles", limitRowHeight: false,
            progress: StatusContext.ProgressTracker());
    }

    public async Task GenerateItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedGenerationChoice == null)
        {
            StatusContext.ToastError("Please make a Generation Date Choice");
            return;
        }

        StatusContext.Progress("Setting up db");

        var db = await Db.Context();

        var generationDirectory =
            new DirectoryInfo(UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName).FullName;

        StatusContext.Progress($"Filtering for Generation Directory: {FilterForFilesInCurrentGenerationDirectory}");

        var searchQuery = FilterForFilesInCurrentGenerationDirectory
            ? db.GenerationFileWriteLogs.Where(x => x.FileName != null && x.FileName.StartsWith(generationDirectory))
            : db.GenerationFileWriteLogs;

        if (SelectedGenerationChoice.FilterDateTimeUtc != null)
        {
            StatusContext.Progress($"Filtering by Date and Time {SelectedGenerationChoice.FilterDateTimeUtc:F}");
            searchQuery = searchQuery.Where(x => x.WrittenOnVersion >= SelectedGenerationChoice.FilterDateTimeUtc);
        }

        var dbItems = await searchQuery.OrderByDescending(x => x.WrittenOnVersion).ToListAsync();

        var transformedItems = new List<FilesWrittenLogListListItem>();

        StatusContext.Progress($"Processing {dbItems.Count} items for display");
        foreach (var loopDbItems in dbItems.Where(x => !string.IsNullOrWhiteSpace(x.FileName)).ToList())
        {
            var directory =
                new DirectoryInfo(UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName);
            var fileBase = loopDbItems.FileName!.Replace(directory.FullName, string.Empty);
            var isInGenerationDirectory = loopDbItems.FileName.StartsWith(generationDirectory);
            var transformedFileName = ToTransformedFileString(fileBase);

            transformedItems.Add(new FilesWrittenLogListListItem
            {
                FileBase = fileBase,
                TransformedFile = transformedFileName,
                WrittenOn = loopDbItems.WrittenOnVersion.ToLocalTime(),
                WrittenFile = loopDbItems.FileName,
                IsInGenerationDirectory = isInGenerationDirectory
            });
        }

        StatusContext.Progress("Transferring items to the UI");

        await ThreadSwitcher.ResumeForegroundAsync();

        if (Items == null)
        {
            Items = new ObservableCollection<FilesWrittenLogListListItem>(transformedItems);
        }
        else
        {
            Items.Clear();
            transformedItems.ForEach(x => Items.Add(x));
        }
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        UserBucketName = UserSettingsSingleton.CurrentSettings().SiteS3Bucket;
        UserBucketRegion = UserSettingsSingleton.CurrentSettings().SiteS3BucketRegion;

        await LoadDateTimeFilterChoices();
    }

    private async Task LoadDateTimeFilterChoices()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Setting up db");

        var db = await Db.Context();

        var logChoiceList = new List<FileWrittenLogListDateTimeFilterChoice>();

        StatusContext.Progress("Adding Generation Dates");

        logChoiceList.AddRange(
            (await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).Take(15).ToListAsync()).Select(x =>
                new FileWrittenLogListDateTimeFilterChoice
                {
                    DisplayText = $"{x.GenerationVersion.ToLocalTime():F} - Html Generated",
                    FilterDateTimeUtc = x.GenerationVersion
                }));

        StatusContext.Progress($"Using {logChoiceList.Count} Generation Dates - Adding Upload Dates");

        logChoiceList.AddRange(
            (await db.GenerationFileTransferScriptLogs.OrderByDescending(x => x.WrittenOnVersion).Take(30)
                .ToListAsync()).Select(x => new FileWrittenLogListDateTimeFilterChoice
            {
                DisplayText = $"{x.WrittenOnVersion.ToLocalTime():F} - Upload Generated",
                FilterDateTimeUtc = x.WrittenOnVersion
            }));

        StatusContext.Progress("Finished adding Filter Date Times - setting up choices");


        logChoiceList = logChoiceList.OrderByDescending(x => x.FilterDateTimeUtc).ToList();

        logChoiceList.Add(new FileWrittenLogListDateTimeFilterChoice { DisplayText = "All" });


        FileWrittenLogListDateTimeFilterChoice toSelect;

        if (SelectedGenerationChoice == null)
        {
            var possibleLastScript = logChoiceList.FirstOrDefault(x => x.DisplayText.EndsWith("  - Upload Generated"));

            toSelect = possibleLastScript ?? logChoiceList[0];
        }
        else
        {
            var possibleCurrentObject = logChoiceList.FirstOrDefault(x =>
                x.FilterDateTimeUtc == SelectedGenerationChoice.FilterDateTimeUtc &&
                x.DisplayText == SelectedGenerationChoice.DisplayText);

            toSelect = possibleCurrentObject ?? logChoiceList[0];
        }

        StatusContext.Progress("Transferring choices to the UI");

        await ThreadSwitcher.ResumeForegroundAsync();

        GenerationChoices ??= new ObservableCollection<FileWrittenLogListDateTimeFilterChoice>();
        GenerationChoices.Clear();
        logChoiceList.ForEach(x => GenerationChoices.Add(x));

        SelectedGenerationChoice = toSelect;
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor.Enqueue(e);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs? e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName is nameof(UserBucketName) or nameof(UserScriptPrefix) or nameof(ChangeSlashes))
            StatusContext.RunBlockingAction(() =>
            {
                var currentItems = Items?.ToList();
                currentItems?.Where(x => x.IsInGenerationDirectory).ToList().ForEach(x =>
                    x.TransformedFile = ToTransformedFileString(x.FileBase));
            });
    }

    private async Task OpenUploaderJsonFile()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting Uploader Json File load.");

        var dialog = new VistaOpenFileDialog
        {
            Multiselect = false,
            Filter = "json files (*.json)|*.json|All files (*.*)|*.*",
            InitialDirectory = UserSettingsSingleton.CurrentSettings().LocalScriptsDirectory().FullName
        };

        if (!(dialog.ShowDialog() ?? false)) return;

        var selectedFileName = dialog.FileNames?.FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(selectedFileName)) return;

        var file = new FileInfo(selectedFileName);

        if (!file.Exists)
        {
            StatusContext.ToastError($"Selected Json Upload File - {selectedFileName} - doesn't exist?");
            return;
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<S3UploadFileRecord>>(
                await File.ReadAllTextAsync(file.FullName));

            if (items == null || !items.Any())
            {
                StatusContext.ToastError("File format error or no items to upload?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var newUploaderWindow =
                new S3UploadsWindow(
                    items.Select(x =>
                        new S3Upload(new FileInfo(x.FileFullName), x.S3Key, x.BucketName, x.Region, x.Note)).ToList(),
                    false);
            newUploaderWindow.PositionWindowAndShow();
        }
        catch (Exception e)
        {
            await StatusContext.ShowMessageWithOkButton("File Import Error", e.ToString());
        }
    }

    private async Task SelectedFilesToExcel()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (Items == null || !Items.Any())
        {
            StatusContext.ToastError("No Items?");
            return;
        }

        FilesToExcel(SelectedItems);
    }

    public async Task SelectedScriptStringsToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems.Any())
        {
            StatusContext.ToastError("No Items Selected?");
            return;
        }

        var scriptString = FileItemsToScriptString(SelectedItems);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(scriptString);
    }

    public async Task SelectedScriptStringsToPowerShellScript()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems.Any())
        {
            StatusContext.ToastError("No Items Selected?");
            return;
        }

        await FileItemsToScriptFile(SelectedItems).ConfigureAwait(true);
    }

    public async Task SelectedWrittenFilesToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (Items == null || !Items.Any())
        {
            StatusContext.ToastError("No Items?");
            return;
        }

        await FilesToClipboard(SelectedItems).ConfigureAwait(true);
    }

    public async Task SelectedWrittenFilesToS3Uploader(bool autoStartUploader)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems.Any())
        {
            StatusContext.ToastError("No Items?");
            return;
        }

        await FileItemsToS3Uploader(SelectedItems, autoStartUploader);
    }

    public async Task SelectedWrittenFilesToS3UploaderJsonFile()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems.Any())
        {
            StatusContext.ToastError("No Items?");
            return;
        }

        await FileItemsToS3UploaderJsonFile(SelectedItems);
    }

    public async Task SiteDeletedFilesReport()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        var results = await AwsS3GeneratedSiteComparisonForDeletions.RunReport(StatusContext.ProgressTracker());

        if (!results.S3KeysToDelete.Any())
        {
            StatusContext.ToastSuccess("Nothing on site found to delete.");
            return;
        }

        if (results.ErrorMessages.Any())
        {
            if (results.S3KeysToDelete.Any())
            {
                var cancelOrContinue = await StatusContext.ShowMessage("Files For Deletion Report Error",
                    $"The report returned {results.S3KeysToDelete.Count} results and the errors below - Cancel or Continue?{Environment.NewLine}{Environment.NewLine}{string.Join($"{{Environment.NewLine}}{Environment.NewLine}", results.ErrorMessages)}",
                    new List<string> { "Cancel", "Continue" });

                if (cancelOrContinue == "Cancel") return;
            }
            else
            {
                await StatusContext.ShowMessageWithOkButton("Files For Deletion Report Error",
                    $"The report returned no items and the following errors:{Environment.NewLine}{Environment.NewLine}{string.Join($"{{Environment.NewLine}}{Environment.NewLine}", results.ErrorMessages)}");
            }
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var newUploadWindow = new S3DeletionsWindow(results.S3KeysToDelete.Select(x =>
            new S3DeletionsItem
            {
                AmazonObjectKey = x, BucketName = UserSettingsSingleton.CurrentSettings().SiteS3Bucket
            }).ToList());

        newUploadWindow.PositionWindowAndShow();
    }

    public async Task SiteMissingAndChangedFilesReport()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        var results =
            await AwsS3GeneratedSiteComparisonForAdditionsAndChanges.RunReport(StatusContext.ProgressTracker());

        if (results.ErrorMessages.Any())
        {
            if (results.FileSizeMismatches.Any() || results.MissingFiles.Any())
            {
                var cancelOrContinue = await StatusContext.ShowMessage("Missing And Changed Files Report Error",
                    $"The report returned {results.FileSizeMismatches.Count + results.MissingFiles.Count} results and the errors below - Cancel or Continue?{Environment.NewLine}{Environment.NewLine}{string.Join($"{{Environment.NewLine}}{Environment.NewLine}", results.ErrorMessages)}",
                    new List<string> { "Cancel", "Continue" });

                if (cancelOrContinue == "Cancel") return;
            }
            else
            {
                await StatusContext.ShowMessageWithOkButton("Missing And Changed Files Report Error",
                    $"The report returned no items and the following errors:{Environment.NewLine}{Environment.NewLine}{string.Join($"{{Environment.NewLine}}{Environment.NewLine}", results.ErrorMessages)}");
                return;
            }
        }

        var toUpload = results.FileSizeMismatches.Concat(results.MissingFiles).ToList();

        if (!toUpload.Any())
        {
            StatusContext.ToastSuccess("No Missing Files or Size Mismatches Found");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var newUploadWindow = new S3UploadsWindow(toUpload, false);
        newUploadWindow.PositionWindowAndShow();
    }

    public string ToTransformedFileString(string fileBase)
    {
        var allPieces = $"{UserBucketName.TrimNullToEmpty()}{fileBase}";
        if (ChangeSlashes) allPieces = allPieces.Replace("\\", "/");

        return allPieces;
    }
}