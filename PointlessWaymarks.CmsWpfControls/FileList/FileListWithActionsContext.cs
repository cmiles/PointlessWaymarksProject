using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private Command _fileImageLinkCodesToClipboardForSelectedCommand;
        private Command _filePageLinkCodesToClipboardForSelectedCommand;
        private Command _firstPagePreviewFromPdfToCairoCommand;
        private ContentListContext _listContext;
        private Command _refreshDataCommand;

        public FileListWithActionsContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
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

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext = new ContentListContext(StatusContext, new FileListLoader(100));

            RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);
            FilePageLinkCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(FilePageLinkCodesToClipboardForSelected);
            FileImageLinkCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(FileImageLinkCodesToClipboardForSelected);
            FileDownloadLinkCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(FileDownloadLinkCodesToClipboardForSelected);

            FirstPagePreviewFromPdfToCairoCommand =
                StatusContext.RunBlockingTaskCommand(FirstPagePreviewFromPdfToCairo);

            EmailHtmlToClipboardCommand = StatusContext.RunBlockingTaskCommand(EmailHtmlToClipboard);

            await ListContext.LoadData();
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
    }
}