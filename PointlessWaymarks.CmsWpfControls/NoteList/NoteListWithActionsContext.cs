using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.ContentHtml.NoteHtml;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.NoteList
{
    public class NoteListWithActionsContext : INotifyPropertyChanged
    {
        private readonly StatusControlContext _statusContext;
        private Command _emailHtmlToClipboardCommand;
        private ContentListContext _listContext;
        private Command _postCodesToClipboardForSelectedCommand;
        private Command _refreshDataCommand;

        public NoteListWithActionsContext(StatusControlContext statusContext)
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

        public Command NoteCodesToClipboardForSelectedCommand
        {
            get => _postCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _postCodesToClipboardForSelectedCommand)) return;
                _postCodesToClipboardForSelectedCommand = value;
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

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext = new ContentListContext(StatusContext, new NoteListLoader(100));

            RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);
            NoteCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(NoteCodesToClipboardForSelected);
            EmailHtmlToClipboardCommand = StatusContext.RunBlockingTaskCommand(EmailHtmlToClipboard);

            await ListContext.LoadData();
        }

        private async Task NoteCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = string.Empty;

            foreach (var loopSelected in SelectedItems())
                finalString +=
                    @$"{{{{notelink {loopSelected.DbEntry.ContentId}; {loopSelected.DbEntry.Slug}}}}}{Environment.NewLine}";

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<NoteListListItem> SelectedItems()
        {
            return ListContext?.ListSelection?.SelectedItems?.Where(x => x is NoteListListItem)
                .Cast<NoteListListItem>()
                .ToList() ?? new List<NoteListListItem>();
        }
    }
}