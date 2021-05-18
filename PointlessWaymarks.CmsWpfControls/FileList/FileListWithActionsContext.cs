using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.FileHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.FileList
{
    public class FileListWithActionsContext : INotifyPropertyChanged
    {
        private Command _deleteSelectedCommand;
        private Command _editSelectedContentCommand;
        private Command _emailHtmlToClipboardCommand;
        private Command _extractNewLinksInSelectedCommand;
        private Command _fileDownloadLinkCodesToClipboardForSelectedCommand;
        private Command _fileImageLinkCodesToClipboardForSelectedCommand;
        private Command _filePageLinkCodesToClipboardForSelectedCommand;
        private Command _firstPagePreviewFromPdfToCairoCommand;
        private Command _generateSelectedHtmlCommand;
        private Command _importFromExcelFileCommand;
        private Command _importFromOpenExcelInstanceCommand;
        private ContentListContext _listContext;
        private Command _newContentCommand;
        private Command _newContentFromFilesCommand;
        private Command _openUrlForSelectedCommand;
        private Command _selectedToExcelCommand;
        private StatusControlContext _statusContext;
        private Command _viewHistoryCommand;

        public FileListWithActionsContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public Command DeleteSelectedCommand
        {
            get => _deleteSelectedCommand;
            set
            {
                if (Equals(value, _deleteSelectedCommand)) return;
                _deleteSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command EditSelectedContentCommand
        {
            get => _editSelectedContentCommand;
            set
            {
                if (Equals(value, _editSelectedContentCommand)) return;
                _editSelectedContentCommand = value;
                OnPropertyChanged();
            }
        }

        public Command EmailHtmlToClipboardCommand
        {
            get => _emailHtmlToClipboardCommand;
            set
            {
                if (Equals(value, _emailHtmlToClipboardCommand)) return;
                _emailHtmlToClipboardCommand = value;
                OnPropertyChanged();
            }
        }


        public Command ExtractNewLinksInSelectedCommand
        {
            get => _extractNewLinksInSelectedCommand;
            set
            {
                if (Equals(value, _extractNewLinksInSelectedCommand)) return;
                _extractNewLinksInSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command FileDownloadLinkCodesToClipboardForSelectedCommand
        {
            get => _fileDownloadLinkCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _fileDownloadLinkCodesToClipboardForSelectedCommand)) return;
                _fileDownloadLinkCodesToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command FileImageLinkCodesToClipboardForSelectedCommand
        {
            get => _fileImageLinkCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _fileImageLinkCodesToClipboardForSelectedCommand)) return;
                _fileImageLinkCodesToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command FilePageLinkCodesToClipboardForSelectedCommand
        {
            get => _filePageLinkCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _filePageLinkCodesToClipboardForSelectedCommand)) return;
                _filePageLinkCodesToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command FirstPagePreviewFromPdfToCairoCommand
        {
            get => _firstPagePreviewFromPdfToCairoCommand;
            set
            {
                if (Equals(value, _firstPagePreviewFromPdfToCairoCommand)) return;
                _firstPagePreviewFromPdfToCairoCommand = value;
                OnPropertyChanged();
            }
        }


        public Command GenerateSelectedHtmlCommand
        {
            get => _generateSelectedHtmlCommand;
            set
            {
                if (Equals(value, _generateSelectedHtmlCommand)) return;
                _generateSelectedHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ImportFromExcelFileCommand
        {
            get => _importFromExcelFileCommand;
            set
            {
                if (Equals(value, _importFromExcelFileCommand)) return;
                _importFromExcelFileCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ImportFromOpenExcelInstanceCommand
        {
            get => _importFromOpenExcelInstanceCommand;
            set
            {
                if (Equals(value, _importFromOpenExcelInstanceCommand)) return;
                _importFromOpenExcelInstanceCommand = value;
                OnPropertyChanged();
            }
        }

        public ContentListContext ListContext
        {
            get => _listContext;
            set
            {
                if (Equals(value, _listContext)) return;
                _listContext = value;
                OnPropertyChanged();
            }
        }

        public Command NewContentCommand
        {
            get => _newContentCommand;
            set
            {
                if (Equals(value, _newContentCommand)) return;
                _newContentCommand = value;
                OnPropertyChanged();
            }
        }

        public Command NewContentFromFilesCommand
        {
            get => _newContentFromFilesCommand;
            set
            {
                if (Equals(value, _newContentFromFilesCommand)) return;
                _newContentFromFilesCommand = value;
                OnPropertyChanged();
            }
        }

        public Command OpenUrlForSelectedCommand
        {
            get => _openUrlForSelectedCommand;
            set
            {
                if (Equals(value, _openUrlForSelectedCommand)) return;
                _openUrlForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command RefreshDataCommand { get; set; }

        public Command SelectedToExcelCommand
        {
            get => _selectedToExcelCommand;
            set
            {
                if (Equals(value, _selectedToExcelCommand)) return;
                _selectedToExcelCommand = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public Command ViewHistoryCommand
        {
            get => _viewHistoryCommand;
            set
            {
                if (Equals(value, _viewHistoryCommand)) return;
                _viewHistoryCommand = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private async Task Delete()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var selected = SelectedItems().OrderBy(x => x.DbEntry.Title).ToList();

            if (!selected.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (selected.Count > 1)
                if (await StatusContext.ShowMessage("Delete Multiple Items",
                    $"You are about to delete {selected.Count} items - do you really want to delete all of these items?" +
                    $"{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, selected.Select(x => x.DbEntry.Title))}",
                    new List<string> {"Yes", "No"}) == "No")
                    return;

            var settings = UserSettingsSingleton.CurrentSettings();

            foreach (var loopSelected in selected)
            {
                if (loopSelected.DbEntry == null || loopSelected.DbEntry.Id < 1)
                {
                    StatusContext.ToastError("Entry is not saved - Skipping?");
                    return;
                }

                await Db.DeleteFileContent(loopSelected.DbEntry.ContentId, StatusContext.ProgressTracker());

                var possibleContentDirectory = settings.LocalSiteFileContentDirectory(loopSelected.DbEntry, false);
                if (possibleContentDirectory.Exists)
                {
                    StatusContext.Progress($"Deleting Generated Folder {possibleContentDirectory.FullName}");
                    possibleContentDirectory.Delete(true);
                }
            }
        }

        private async Task EditSelectedContent()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var context = await Db.Context();
            var frozenList = SelectedItems();

            foreach (var loopSelected in frozenList)
            {
                var refreshedData =
                    context.FileContents.SingleOrDefault(x => x.ContentId == loopSelected.DbEntry.ContentId);

                if (refreshedData == null)
                {
                    StatusContext.ToastError(
                        $"{loopSelected.DbEntry.Title} is no longer active in the database? Can not edit - " +
                        "look for a historic version...");
                    continue;
                }

                await ThreadSwitcher.ResumeForegroundAsync();

                var newContentWindow = new FileContentEditorWindow(refreshedData);

                newContentWindow.PositionWindowAndShow();

                await ThreadSwitcher.ResumeBackgroundAsync();
            }
        }

        private async Task EmailHtmlToClipboard()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (SelectedItems().Count > 1)
            {
                StatusContext.ToastError("Please select only 1 item...");
                return;
            }

            var frozenSelected = SelectedItems().First();

            var emailHtml = await Email.ToHtmlEmail(frozenSelected.DbEntry, StatusContext.ProgressTracker());

            await ThreadSwitcher.ResumeForegroundAsync();

            HtmlClipboardHelpers.CopyToClipboard(emailHtml, emailHtml);

            StatusContext.ToastSuccess("Email Html on Clipboard");
        }

        private async Task ExtractNewLinksInSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var context = await Db.Context();
            var frozenList = SelectedItems();

            foreach (var loopSelected in frozenList)
            {
                var refreshedData =
                    context.FileContents.SingleOrDefault(x => x.ContentId == loopSelected.DbEntry.ContentId);

                if (refreshedData == null) continue;

                await LinkExtraction.ExtractNewAndShowLinkContentEditors(
                    $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
            }
        }

        private async Task FileDownloadLinkCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = string.Empty;

            foreach (var loopSelected in SelectedItems())
                finalString += @$"{BracketCodeFileDownloads.Create(loopSelected.DbEntry)}{Environment.NewLine}";

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task FileImageLinkCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = string.Empty;

            foreach (var loopSelected in SelectedItems())
                finalString += @$"{BracketCodeFileImage.Create(loopSelected.DbEntry)}{Environment.NewLine}";

            if (SelectedItems().Any(x => x.DbEntry.MainPicture == null))
                StatusContext.ToastWarning("Some File Image Links do not have images?");

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task FilePageLinkCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = string.Empty;

            foreach (var loopSelected in SelectedItems())
                finalString += @$"{BracketCodeFiles.Create(loopSelected.DbEntry)}{Environment.NewLine}";

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task FirstPagePreviewFromPdfToCairo()
        {
            var selected = SelectedItems();

            if (selected == null || !selected.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            await PdfHelpers.PdfPageToImageWithPdfToCairo(StatusContext, selected.Select(x => x.DbEntry).ToList(), 1);
        }


        private async Task GenerateSelectedHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var loopCount = 1;
            var totalCount = SelectedItems().Count;

            foreach (var loopSelected in SelectedItems())
            {
                StatusContext.Progress(
                    $"Generating Html for {loopSelected.DbEntry.Title}, {loopCount} of {totalCount}");

                var htmlContext = new SingleFilePage(loopSelected.DbEntry);

                htmlContext.WriteLocalHtml();

                StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");

                loopCount++;
            }
        }


        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext = new ContentListContext(StatusContext, new FileListLoader(100));

            RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);
            GenerateSelectedHtmlCommand = StatusContext.RunBlockingTaskCommand(GenerateSelectedHtml);
            EditSelectedContentCommand = StatusContext.RunBlockingTaskCommand(EditSelectedContent);
            FilePageLinkCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(FilePageLinkCodesToClipboardForSelected);
            FileImageLinkCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(FileImageLinkCodesToClipboardForSelected);
            FileDownloadLinkCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(FileDownloadLinkCodesToClipboardForSelected);
            OpenUrlForSelectedCommand = StatusContext.RunNonBlockingTaskCommand(OpenUrlForSelected);
            NewContentCommand = StatusContext.RunNonBlockingTaskCommand(NewContent);

            NewContentFromFilesCommand =
                StatusContext.RunBlockingTaskWithCancellationCommand(async x => await NewContentFromFiles(x),
                    "Cancel File Import");

            DeleteSelectedCommand = StatusContext.RunBlockingTaskCommand(Delete);
            FirstPagePreviewFromPdfToCairoCommand =
                StatusContext.RunBlockingTaskCommand(FirstPagePreviewFromPdfToCairo);
            ExtractNewLinksInSelectedCommand = StatusContext.RunBlockingTaskCommand(ExtractNewLinksInSelected);
            ViewHistoryCommand = StatusContext.RunNonBlockingTaskCommand(ViewHistory);

            EmailHtmlToClipboardCommand = StatusContext.RunBlockingTaskCommand(EmailHtmlToClipboard);

            ImportFromExcelFileCommand =
                StatusContext.RunBlockingTaskCommand(async () => await ExcelHelpers.ImportFromExcelFile(StatusContext));
            ImportFromOpenExcelInstanceCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await ExcelHelpers.ImportFromOpenExcelInstance(StatusContext));
            SelectedToExcelCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
                await ExcelHelpers.SelectedToExcel(SelectedItems()?.Cast<dynamic>().ToList(),
                    StatusContext));

            await ListContext.LoadData();
        }

        private async Task NewContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new FileContentEditorWindow();

            newContentWindow.PositionWindowAndShow();
        }

        private async Task NewContentFromFiles(CancellationToken cancellationToken)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Starting File load.");

            var dialog = new VistaOpenFileDialog {Multiselect = true};

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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OpenUrlForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var settings = UserSettingsSingleton.CurrentSettings();

            foreach (var loopSelected in SelectedItems())
            {
                var url = $@"http://{settings.FilePageUrl(loopSelected.DbEntry)}";

                var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
                Process.Start(ps);
            }
        }

        public List<FileListListItem> SelectedItems()
        {
            return ListContext?.ListSelection?.SelectedItems?.Where(x => x is FileListListItem)
                .Cast<FileListListItem>()
                .ToList() ?? new List<FileListListItem>();
        }

        private async Task ViewHistory()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var selected = SelectedItems();

            if (selected == null || !selected.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (selected.Count > 1)
            {
                StatusContext.ToastError("Please Select a Single Item");
                return;
            }

            var singleSelected = selected.Single();

            if (singleSelected.DbEntry == null || singleSelected.DbEntry.ContentId == Guid.Empty)
            {
                StatusContext.ToastWarning("No History - New/Unsaved Entry?");
                return;
            }

            var db = await Db.Context();

            StatusContext.Progress($"Looking up Historic Entries for {singleSelected.DbEntry.Title}");

            var historicItems = await db.HistoricFileContents
                .Where(x => x.ContentId == singleSelected.DbEntry.ContentId).ToListAsync();

            StatusContext.Progress($"Found {historicItems.Count} Historic Entries");

            if (historicItems.Count < 1)
            {
                StatusContext.ToastWarning("No History to Show...");
                return;
            }

            var historicView = new ContentViewHistoryPage($"Historic Entries - {singleSelected.DbEntry.Title}",
                UserSettingsSingleton.CurrentSettings().SiteName, $"Historic Entries - {singleSelected.DbEntry.Title}",
                historicItems.OrderByDescending(x => x.LastUpdatedOn.HasValue).ThenByDescending(x => x.LastUpdatedOn)
                    .Select(ObjectDumper.Dump).ToList());

            historicView.WriteHtmlToTempFolderAndShow(StatusContext.ProgressTracker());
        }
    }
}