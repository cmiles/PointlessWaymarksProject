using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.FileHtml;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.FileList
{
    public class FileListWithActionsContext : INotifyPropertyChanged
    {
        private readonly StatusControlContext _statusContext;
        private Command _emailHtmlToClipboardCommand;
        private Command _fileDownloadLinkCodesToClipboardForSelectedCommand;
        private Command _filePageLinkCodesToClipboardForSelectedCommand;
        private Command _firstPagePreviewFromPdfToCairoCommand;
        private ContentListContext _listContext;
        private Command _refreshDataCommand;
        private Command _viewFilesCommand;
        private Command _fileUrlLinkCodesToClipboardForSelectedCommand;

        public FileListWithActionsContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTask(LoadData);
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

        public Command RefreshDataCommand
        {
            get => _refreshDataCommand;
            set
            {
                if (Equals(value, _refreshDataCommand)) return;
                _refreshDataCommand = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            private init
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public Command ViewFilesCommand
        {
            get => _viewFilesCommand;
            set
            {
                if (Equals(value, _viewFilesCommand)) return;
                _viewFilesCommand = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        private async Task FileUrlLinkCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = string.Empty;

            foreach (var loopSelected in SelectedItems())
                finalString += @$"{BracketCodeFileUrl.Create(loopSelected.DbEntry)}{Environment.NewLine}";

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

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext ??= new ContentListContext(StatusContext, new FileListLoader(100));

            RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);
            FilePageLinkCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(FilePageLinkCodesToClipboardForSelected);
            FileDownloadLinkCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(FileDownloadLinkCodesToClipboardForSelected);
            FileUrlLinkCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(FileDownloadLinkCodesToClipboardForSelected);
            ViewFilesCommand =
                StatusContext.RunBlockingTaskWithCancellationCommand(ViewFilesSelected, "Cancel File View");

            FirstPagePreviewFromPdfToCairoCommand =
                StatusContext.RunBlockingTaskCommand(FirstPagePreviewFromPdfToCairo);

            EmailHtmlToClipboardCommand = StatusContext.RunBlockingTaskCommand(EmailHtmlToClipboard);

            ListContext.ContextMenuItems = new List<ContextMenuItemData>
            {
                new() {ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand},
                new()
                {
                    ItemName = "Image Code to Clipboard", ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
                },
                new()
                {
                    ItemName = "Text Code to Clipboard",
                    ItemCommand = FilePageLinkCodesToClipboardForSelectedCommand
                },
                new()
                {
                    ItemName = "Download Code to Clipboard",
                    ItemCommand = FileDownloadLinkCodesToClipboardForSelectedCommand
                },
                new()
                {
                    ItemName = "URL Code to Clipboard",
                    ItemCommand = FileUrlLinkCodesToClipboardForSelectedCommand
                },
                new() {ItemName = "Email Html to Clipboard", ItemCommand = EmailHtmlToClipboardCommand},
                new() {ItemName = "View Files", ItemCommand = ViewFilesCommand},
                new() {ItemName = "Open URLs", ItemCommand = ListContext.OpenUrlSelectedCommand},
                new()
                    {ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand},
                new()
                {
                    ItemName = "Generate Html",
                    ItemCommand = ListContext.GenerateHtmlSelectedCommand
                },
                new() {ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand},
                new()
                    {ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand},
                new()
                    {ItemName = "Refresh Data", ItemCommand = RefreshDataCommand}
            };

            await ListContext.LoadData();
        }

        public Command FileUrlLinkCodesToClipboardForSelectedCommand
        {
            get => _fileUrlLinkCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _fileUrlLinkCodesToClipboardForSelectedCommand)) return;
                _fileUrlLinkCodesToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<FileListListItem> SelectedItems()
        {
            return ListContext?.ListSelection?.SelectedItems?.Where(x => x is FileListListItem).Cast<FileListListItem>()
                .ToList() ?? new List<FileListListItem>();
        }

        public async Task ViewFilesSelected(CancellationToken cancelToken)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.ListSelection?.SelectedItems == null || ListContext.ListSelection.SelectedItems.Count < 1)
            {
                StatusContext.ToastWarning("Nothing Selected to View?");
                return;
            }

            if (ListContext.ListSelection.SelectedItems.Count > 20)
            {
                StatusContext.ToastWarning("Sorry - please select less than 20 items to view...");
                return;
            }

            var currentSelected = ListContext.ListSelection.SelectedItems;

            foreach (var loopSelected in currentSelected)
            {
                cancelToken.ThrowIfCancellationRequested();

                if (loopSelected is FileListListItem fileItem)
                    await fileItem.ItemActions.ViewFile(fileItem.DbEntry);
            }
        }
    }
}