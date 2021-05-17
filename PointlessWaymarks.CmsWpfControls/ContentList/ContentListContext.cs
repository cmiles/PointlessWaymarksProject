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
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ColumnSort;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.ImageList;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.LinkList;
using PointlessWaymarks.CmsWpfControls.MapComponentList;
using PointlessWaymarks.CmsWpfControls.NoteList;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CmsWpfControls.PostList;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Commands;
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
        private LinkListItemActions _linkItemActions;
        private ContentListSelected<IContentListItem> _listSelection;
        private ColumnSortControlContext _listSort;
        private MapComponentListItemActions _mapComponentItemActions;
        private NoteListItemAction _noteItemActions;
        private int? _partialLoadQuantity;
        private PhotoListItemActions _photoItemActions;
        private PointListItemActions _pointItemActions;
        private PostListItemActions _postItemActions;
        private IContentListItem _selectedItem;
        private StatusControlContext _statusContext;
        private string _userFilterText;
        private bool _allItemsLoaded;
        private Command _loadAllCommand;

        public ContentListContext(StatusControlContext statusContext,
            Func<int?, IProgress<string>, Task<List<object>>> loadItemsFunction, Func<int?, Task<bool>> allItemsLoadedCheck,
            int? partialLoadQuantity)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            PartialLoadQuantity = partialLoadQuantity;
            LoadItemsFunction = loadItemsFunction;
            AllItemsLoadedCheck = allItemsLoadedCheck;

            FileItemActions = new FileListItemActions(StatusContext);
            GeoJasonItemActions = new GeoJsonListItemActions(StatusContext);
            ImageItemActions = new ImageListItemActions(StatusContext);
            LineItemActions = new LineListItemActions(StatusContext);
            LinkItemActions = new LinkListItemActions(StatusContext);
            MapComponentItemActions = new MapComponentListItemActions(StatusContext);
            NoteItemActions = new NoteListItemAction(StatusContext);
            PointItemActions = new PointListItemActions(StatusContext);
            PhotoItemActions = new PhotoListItemActions(StatusContext);
            PostItemActions = new PostListItemActions(StatusContext);

            DataNotificationsProcessor = new DataNotificationsWorkQueue {Processor = DataNotificationReceived};

            LoadAllCommand = StatusContext.RunBlockingTaskCommand(async () =>
            {
                PartialLoadQuantity = null;
                await LoadData();
            });
        }

        public Command LoadAllCommand
        {
            get => _loadAllCommand;
            set
            {
                if (Equals(value, _loadAllCommand)) return;
                _loadAllCommand = value;
                OnPropertyChanged();
            }
        }

        public bool AllItemsLoaded
        {
            get => _allItemsLoaded;
            set
            {
                if (value == _allItemsLoaded) return;
                _allItemsLoaded = value;
                OnPropertyChanged();
            }
        }

        public Func<int?, Task<bool>> AllItemsLoadedCheck { get; set; }

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

        public LinkListItemActions LinkItemActions
        {
            get => _linkItemActions;
            set
            {
                if (Equals(value, _linkItemActions)) return;
                _linkItemActions = value;
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

        public Func<int?, IProgress<string>, Task<List<object>>> LoadItemsFunction { get; set; }

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

        public int? PartialLoadQuantity
        {
            get => _partialLoadQuantity;
            set
            {
                if (value == _partialLoadQuantity) return;
                _partialLoadQuantity = value;
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

        public IContentListItem ListItemFromDbItem(object dbItem)
        {
            return dbItem switch
            {
                FileContent f => FileListItemActions.ListItemFromDbItem(f, FileItemActions),
                GeoJsonContent g => GeoJsonListItemActions.ListItemFromDbItem(g, GeoJasonItemActions),
                LineContent l => LineListItemActions.ListItemFromDbItem(l, LineItemActions),
                LinkContent k => LinkListItemActions.ListItemFromDbItem(k, LinkItemActions),
                MapComponent m => MapComponentListItemActions.ListItemFromDbItem(m, MapComponentItemActions),
                NoteContent n => NoteListItemAction.ListItemFromDbItem(n, NoteItemActions),
                PhotoContent ph => PhotoListItemActions.ListItemFromDbItem(ph, PhotoItemActions),
                PointContent pt => PointListItemActions.ListItemFromDbItem(pt, PointItemActions),
                PostContent po => PostListItemActions.ListItemFromDbItem(po, PostItemActions),
                _ => null
            };
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListSelection = await ContentListSelected<IContentListItem>.CreateInstance(StatusContext);

            //DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

            StatusContext.Progress("Setting up Sorting");
            
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

            StatusContext.Progress("Starting Item Load");
            
            var dbItems = LoadItemsFunction == null
                ? new List<object>()
                : await LoadItemsFunction(PartialLoadQuantity, StatusContext.ProgressTracker());

            StatusContext.Progress($"Checking for All Items Loaded from Db");

            if (PartialLoadQuantity == null)
            {
                AllItemsLoaded = true;
            }
            else
            {
                if (AllItemsLoadedCheck == null) AllItemsLoaded = true;
                else AllItemsLoaded = await AllItemsLoadedCheck(PartialLoadQuantity);
            }
            
            StatusContext.Progress($"All Items Loaded from Db: {AllItemsLoaded}");

            var contentListItems = new List<IContentListItem>();

            StatusContext.Progress($"Creating List Items");
            
            var loopCounter = 0;
            foreach(var loopDbItem in dbItems)
            {
                if(loopCounter++ % 250 == 0) StatusContext.Progress($"Created List Item {loopCounter} of {dbItems.Count}");
                contentListItems.Add(ListItemFromDbItem(loopDbItem));
            }
            
            contentListItems = dbItems.Select(ListItemFromDbItem).ToList();

            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Loading Display List of Items");

            Items = new ObservableCollection<IContentListItem>(contentListItems);

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