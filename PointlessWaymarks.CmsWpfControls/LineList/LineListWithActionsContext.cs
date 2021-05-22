using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.LineList
{
    public class LineListWithActionsContext : INotifyPropertyChanged
    {
        private readonly StatusControlContext _statusContext;
        private Command _lineLinkCodesToClipboardForSelectedCommand;
        private Command _lineMapCodesToClipboardForSelectedCommand;
        private ContentListContext _listContext;
        private Command _refreshDataCommand;

        public LineListWithActionsContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public Command LineLinkCodesToClipboardForSelectedCommand
        {
            get => _lineLinkCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _lineLinkCodesToClipboardForSelectedCommand)) return;
                _lineLinkCodesToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command LineMapCodesToClipboardForSelectedCommand
        {
            get => _lineMapCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _lineMapCodesToClipboardForSelectedCommand)) return;
                _lineMapCodesToClipboardForSelectedCommand = value;
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


        private async Task LinkBracketCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = SelectedItems().Aggregate(string.Empty,
                (current, loopSelected) =>
                    current + @$"{BracketCodeLines.Create(loopSelected.DbEntry)}{Environment.NewLine}");

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext = new ContentListContext(StatusContext, new LineListLoader(100));

            LineLinkCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(LinkBracketCodesToClipboardForSelected);
            LineMapCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(MapBracketCodesToClipboardForSelected);
            RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);

            await ListContext.LoadData();
        }

        private async Task MapBracketCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = SelectedItems().Aggregate(string.Empty,
                (current, loopSelected) =>
                    current + @$"{BracketCodeLines.Create(loopSelected.DbEntry)}{Environment.NewLine}");

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<LineListListItem> SelectedItems()
        {
            return ListContext?.ListSelection?.SelectedItems?.Where(x => x is LineListListItem)
                .Cast<LineListListItem>()
                .ToList() ?? new List<LineListListItem>();
        }
    }
}