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

namespace PointlessWaymarks.CmsWpfControls.GeoJsonList
{
    public class GeoJsonListWithActionsContext : INotifyPropertyChanged
    {
        private Command _geoJsonLinkCodesToClipboardForSelectedCommand;
        private Command _geoJsonMapCodesToClipboardForSelectedCommand;
        private ContentListContext _listContext;
        private Command _refreshDataCommand;
        private StatusControlContext _statusContext;

        public GeoJsonListWithActionsContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public Command GeoJsonLinkCodesToClipboardForSelectedCommand
        {
            get => _geoJsonLinkCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _geoJsonLinkCodesToClipboardForSelectedCommand)) return;
                _geoJsonLinkCodesToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command GeoJsonMapCodesToClipboardForSelectedCommand
        {
            get => _geoJsonMapCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _geoJsonMapCodesToClipboardForSelectedCommand)) return;
                _geoJsonMapCodesToClipboardForSelectedCommand = value;
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
            set
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
                    current + @$"{BracketCodeGeoJsonLinks.Create(loopSelected.DbEntry)}{Environment.NewLine}");

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext = new ContentListContext(StatusContext, new GeoJsonLoader(100));

            GeoJsonMapCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(MapBracketCodesToClipboardForSelected);
            GeoJsonLinkCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(LinkBracketCodesToClipboardForSelected);
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
                    current + @$"{BracketCodeGeoJson.Create(loopSelected.DbEntry)}{Environment.NewLine}");

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<GeoJsonListListItem> SelectedItems()
        {
            return ListContext?.ListSelection?.SelectedItems?.Where(x => x is GeoJsonListListItem)
                .Cast<GeoJsonListListItem>()
                .ToList() ?? new List<GeoJsonListListItem>();
        }
    }
}