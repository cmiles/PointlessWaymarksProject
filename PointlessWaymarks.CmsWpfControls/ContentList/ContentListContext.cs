using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ColumnSort;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.ImageList;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.MapComponentList;
using PointlessWaymarks.CmsWpfControls.NoteList;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CmsWpfControls.PostList;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.ContentList
{
    public class ContentListContext : INotifyPropertyChanged
    {
        private FileListItemActions _fileItemActions;
        private GeoJsonListItemActions _geoJasonItemActions;
        private ImageListItemActions _imageItemActions;
        private ObservableCollection<IContentListItem> _items;
        private LineListItemActions _lineItemActions;
        private ContentListSelected<IContentListItem> _listSelection;
        private ColumnSortControlContext _listSort;
        private MapComponentListItemActions _mapComponentItemActions;
        private NoteListItemAction _noteItemActions;
        private PhotoListItemActions _photoItemActions;
        private PointListItemActions _pointItemActions;
        private PostListItemActions _postItemActions;
        private IContentListItem _selectedItem;
        private StatusControlContext _statusContext;
        private string _userFilterText;

        public ContentListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            FileItemActions = new FileListItemActions(StatusContext);
            GeoJasonItemActions = new GeoJsonListItemActions(StatusContext);
            ImageItemActions = new ImageListItemActions(StatusContext);
            LineItemActions = new LineListItemActions(StatusContext);
            MapComponentItemActions = new MapComponentListItemActions(StatusContext);
            NoteItemActions = new NoteListItemAction(StatusContext);
            PointItemActions = new PointListItemActions(StatusContext);
            PhotoItemActions = new PhotoListItemActions(StatusContext);
            PostItemActions = new PostListItemActions(StatusContext);

            DataNotificationsProcessor = new DataNotificationsWorkQueue {Processor = DataNotificationReceived};
        }

        public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }

        public FileListItemActions FileItemActions
        {
            get => _fileItemActions;
            set
            {
                if (Equals(value, _fileItemActions)) return;
                _fileItemActions = value;
                OnPropertyChanged();
            }
        }

        public GeoJsonListItemActions GeoJasonItemActions
        {
            get => _geoJasonItemActions;
            set
            {
                if (Equals(value, _geoJasonItemActions)) return;
                _geoJasonItemActions = value;
                OnPropertyChanged();
            }
        }

        public ImageListItemActions ImageItemActions
        {
            get => _imageItemActions;
            set
            {
                if (Equals(value, _imageItemActions)) return;
                _imageItemActions = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<IContentListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public LineListItemActions LineItemActions
        {
            get => _lineItemActions;
            set
            {
                if (Equals(value, _lineItemActions)) return;
                _lineItemActions = value;
                OnPropertyChanged();
            }
        }

        public ContentListSelected<IContentListItem> ListSelection
        {
            get => _listSelection;
            set
            {
                if (Equals(value, _listSelection)) return;
                _listSelection = value;
                OnPropertyChanged();
            }
        }

        public ColumnSortControlContext ListSort
        {
            get => _listSort;
            set
            {
                if (Equals(value, _listSort)) return;
                _listSort = value;
                OnPropertyChanged();
            }
        }

        public Func<List<IContentListItem>> LoadFunction { get; set; }

        public MapComponentListItemActions MapComponentItemActions
        {
            get => _mapComponentItemActions;
            set
            {
                if (Equals(value, _mapComponentItemActions)) return;
                _mapComponentItemActions = value;
                OnPropertyChanged();
            }
        }

        public NoteListItemAction NoteItemActions
        {
            get => _noteItemActions;
            set
            {
                if (Equals(value, _noteItemActions)) return;
                _noteItemActions = value;
                OnPropertyChanged();
            }
        }

        public PhotoListItemActions PhotoItemActions
        {
            get => _photoItemActions;
            set
            {
                if (Equals(value, _photoItemActions)) return;
                _photoItemActions = value;
                OnPropertyChanged();
            }
        }

        public PointListItemActions PointItemActions
        {
            get => _pointItemActions;
            set
            {
                if (Equals(value, _pointItemActions)) return;
                _pointItemActions = value;
                OnPropertyChanged();
            }
        }

        public PostListItemActions PostItemActions
        {
            get => _postItemActions;
            set
            {
                if (Equals(value, _postItemActions)) return;
                _postItemActions = value;
                OnPropertyChanged();
            }
        }

        public IContentListItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (Equals(value, _selectedItem)) return;
                _selectedItem = value;
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

        public string UserFilterText
        {
            get => _userFilterText;
            set
            {
                if (value == _userFilterText) return;
                _userFilterText = value;
                OnPropertyChanged();

                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(FilterList);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private Task DataNotificationReceived(TinyMessageReceivedEventArgs arg)
        {
            throw new NotImplementedException();
        }

        private async Task FilterList()
        {
            if (Items == null || !Items.Any()) return;

            await ThreadSwitcher.ResumeForegroundAsync();

            ((CollectionView) CollectionViewSource.GetDefaultView(Items)).Filter = o =>
            {
                if (string.IsNullOrWhiteSpace(UserFilterText)) return true;

                var loweredString = UserFilterText.ToLower();

                if (!(o is IContentListItem toFilter)) return false;
                if ((toFilter.Content().Title ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((toFilter.Content().Tags ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((toFilter.Content().Summary ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((toFilter.Content().CreatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((toFilter.Content().LastUpdatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                return false;
            };
        }

        public static async Task<List<IContentListItem>> LoadAll(ContentListContext context, int? partialLoadItems)
        {
            var listItems = new List<IContentListItem>();

            var db = await Db.Context();

            if (partialLoadItems != null)
            {
                listItems.AddRange(
                    (await db.FileContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync()).Select(x =>
                        FileListItemActions.ListItemFromDbItem(x, context.FileItemActions)));
                listItems.AddRange(
                    (await db.GeoJsonContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync()).Select(x =>
                        GeoJsonListItemActions.ListItemFromDbItem(x, context.GeoJasonItemActions)));
                listItems.AddRange(
                    (await db.LineContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync()).Select(x =>
                        LineListItemActions.ListItemFromDbItem(x, context.LineItemActions)));
                listItems.AddRange(
                    (await db.MapComponents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync()).Select(x =>
                        MapComponentListItemActions.ListItemFromDbItem(x, context.MapComponentItemActions)));
                listItems.AddRange(
                    (await db.NoteContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync()).Select(x =>
                        NoteListItemAction.ListItemFromDbItem(x, context.NoteItemActions)));
                listItems.AddRange(
                    (await db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync()).Select(x =>
                        PhotoListItemActions.ListItemFromDbItem(x, context.PhotoItemActions)));
                listItems.AddRange(
                    (await db.PointContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync()).Select(x =>
                        PointListItemActions.ListItemFromDbItem(x, context.PointItemActions)));
                listItems.AddRange(
                    (await db.PostContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                        .Take(partialLoadItems.Value).ToListAsync()).Select(x =>
                        PostListItemActions.ListItemFromDbItem(x, context.PostItemActions)));

                return listItems;
            }


            listItems.AddRange(
                (await db.FileContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync()).Select(x =>
                    FileListItemActions.ListItemFromDbItem(x, context.FileItemActions)));
            listItems.AddRange(
                (await db.GeoJsonContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync()).Select(x =>
                    GeoJsonListItemActions.ListItemFromDbItem(x, context.GeoJasonItemActions)));
            listItems.AddRange(
                (await db.LineContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync()).Select(x =>
                    LineListItemActions.ListItemFromDbItem(x, context.LineItemActions)));
            listItems.AddRange(
                (await db.MapComponents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync()).Select(x =>
                    MapComponentListItemActions.ListItemFromDbItem(x, context.MapComponentItemActions)));
            listItems.AddRange(
                (await db.NoteContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync()).Select(x =>
                    NoteListItemAction.ListItemFromDbItem(x, context.NoteItemActions)));
            listItems.AddRange(
                (await db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync()).Select(x =>
                    PhotoListItemActions.ListItemFromDbItem(x, context.PhotoItemActions)));
            listItems.AddRange(
                (await db.PointContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync()).Select(x =>
                    PointListItemActions.ListItemFromDbItem(x, context.PointItemActions)));
            listItems.AddRange(
                (await db.PostContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .ToListAsync()).Select(x =>
                    PostListItemActions.ListItemFromDbItem(x, context.PostItemActions)));

            return listItems;
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListSelection = await ContentListSelected<IContentListItem>.CreateInstance(StatusContext);

            //DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

            ListSort = new ColumnSortControlContext
            {
                Items = new List<ColumnSortControlSortItem>
                {
                    new()
                    {
                        DisplayName = "Latest Update",
                        ColumnName = "DbEntry.LatestUpdate",
                        Order = 1,
                        DefaultSortDirection = ListSortDirection.Descending
                    },
                    new()
                    {
                        DisplayName = "Title",
                        ColumnName = "DbEntry.Title",
                        DefaultSortDirection = ListSortDirection.Ascending
                    },
                    new()
                    {
                        DisplayName = "Created On",
                        ColumnName = "DbEntry.CreatedOn",
                        DefaultSortDirection = ListSortDirection.Descending
                    }
                }
            };

            ListSort.SortUpdated += (sender, list) =>
                Dispatcher.CurrentDispatcher.Invoke(() => { ListContextSortHelpers.SortList(list, Items); });

            var listItems = await LoadAll(this, 10);

            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Loading Display List of Photos");

            Items = new ObservableCollection<IContentListItem>(listItems);

            ListContextSortHelpers.SortList(ListSort.SortDescriptions(), Items);
            await FilterList();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}