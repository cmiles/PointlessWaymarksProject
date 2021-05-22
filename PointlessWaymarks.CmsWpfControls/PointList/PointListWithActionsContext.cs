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

namespace PointlessWaymarks.CmsWpfControls.PointList
{
    public class PointListWithActionsContext : INotifyPropertyChanged
    {
        private readonly StatusControlContext _statusContext;
        private ContentListContext _listContext;
        private Command _pointLinkBracketCodesToClipboardForSelectedCommand;
        private Command _pointMapBracketCodesToClipboardForSelectedCommand;
        private Command _refreshDataCommand;

        public PointListWithActionsContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
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

        public Command PointLinkBracketCodesToClipboardForSelectedCommand
        {
            get => _pointLinkBracketCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _pointLinkBracketCodesToClipboardForSelectedCommand)) return;
                _pointLinkBracketCodesToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command PointMapBracketCodesToClipboardForSelectedCommand
        {
            get => _pointMapBracketCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _pointMapBracketCodesToClipboardForSelectedCommand)) return;
                _pointMapBracketCodesToClipboardForSelectedCommand = value;
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

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext = new ContentListContext(StatusContext, new PointListLoader(100));

            RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);
            PointLinkBracketCodesToClipboardForSelectedCommand =
                StatusContext.RunNonBlockingTaskCommand(PointLinkBracketCodesToClipboardForSelected);
            PointMapBracketCodesToClipboardForSelectedCommand =
                StatusContext.RunNonBlockingTaskCommand(PointBracketCodesToClipboardForSelected);

            await ListContext.LoadData();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task PointBracketCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = SelectedItems().Aggregate(string.Empty,
                (current, loopSelected) =>
                    current + @$"{BracketCodePoints.Create(loopSelected.DbEntry)}{Environment.NewLine}");

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task PointLinkBracketCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = SelectedItems().Aggregate(string.Empty,
                (current, loopSelected) =>
                    current + @$"{BracketCodePointLinks.Create(loopSelected.DbEntry)}{Environment.NewLine}");

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        public List<PointListListItem> SelectedItems()
        {
            return ListContext?.ListSelection?.SelectedItems?.Where(x => x is PointListListItem)
                .Cast<PointListListItem>()
                .ToList() ?? new List<PointListListItem>();
        }
    }
}