using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapComponentList
{
    public class MapComponentListWithActionsContext : INotifyPropertyChanged
    {
        private readonly StatusControlContext _statusContext;
        private ContentListContext _listContext;
        private Command _refreshDataCommand;
        private WindowIconStatus _windowStatus;

        public MapComponentListWithActionsContext(StatusControlContext statusContext,
            WindowIconStatus windowStatus = null)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            WindowStatus = windowStatus;

            StatusContext.RunFireAndForgetBlockingTask(LoadData);
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

        public WindowIconStatus WindowStatus
        {
            get => _windowStatus;
            set
            {
                if (Equals(value, _windowStatus)) return;
                _windowStatus = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext ??= new ContentListContext(StatusContext, new MapComponentListLoader(100), WindowStatus);

            RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);

            ListContext.ContextMenuItems = new List<ContextMenuItemData>
            {
                new() { ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand },
                new()
                {
                    ItemName = "Map Code to Clipboard",
                    ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
                },
                new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
                new() { ItemName = "Open URL", ItemCommand = ListContext.OpenUrlSelectedCommand },
                new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
                new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
                new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
            };

            await ListContext.LoadData();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<MapComponentListListItem> SelectedItems()
        {
            return ListContext?.ListSelection?.SelectedItems?.Where(x => x is MapComponentListListItem)
                .Cast<MapComponentListListItem>().ToList() ?? new List<MapComponentListListItem>();
        }
    }
}