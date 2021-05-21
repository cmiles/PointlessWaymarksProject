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
using PointlessWaymarks.CmsData.CommonHtml;
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
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.ContentList
{
    public class ContentListContext : INotifyPropertyChanged
    {
        private IContentListLoader _contentListLoader;
        private FileListItemActions _fileItemActions;
        private GeoJsonListItemActions _geoJasonItemActions;
        private ImageListItemActions _imageItemActions;
        private ObservableCollection<IContentListItem> _items;
        private LineListItemActions _lineItemActions;
        private LinkListItemActions _linkItemActions;
        private ContentListSelected<IContentListItem> _listSelection;
        private ColumnSortControlContext _listSort;
        private Command _loadAllCommand;
        private MapComponentListItemActions _mapComponentItemActions;
        private NoteListItemActions _noteItemActions;
        private PhotoListItemActions _photoItemActions;
        private PointListItemActions _pointItemActions;
        private PostListItemActions _postItemActions;
        private StatusControlContext _statusContext;
        private string _userFilterText;
        private NewContent _newActions;
        private Command _editSelectedCommand;
        private Command _deleteSelectedCommand;

        public ContentListContext(StatusControlContext statusContext, IContentListLoader loader)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            ContentListLoader = loader;

            FileItemActions = new FileListItemActions(StatusContext);
            GeoJasonItemActions = new GeoJsonListItemActions(StatusContext);
            ImageItemActions = new ImageListItemActions(StatusContext);
            LineItemActions = new LineListItemActions(StatusContext);
            LinkItemActions = new LinkListItemActions(StatusContext);
            MapComponentItemActions = new MapComponentListItemActions(StatusContext);
            NoteItemActions = new NoteListItemActions(StatusContext);
            PointItemActions = new PointListItemActions(StatusContext);
            PhotoItemActions = new PhotoListItemActions(StatusContext);
            PostItemActions = new PostListItemActions(StatusContext);

            NewActions = new NewContent(StatusContext);

            DataNotificationsProcessor = new DataNotificationsWorkQueue {Processor = DataNotificationReceived};

            LoadAllCommand = StatusContext.RunBlockingTaskCommand(async () =>
            {
                ContentListLoader.PartialLoadQuantity = null;
                await LoadData();
            });
            EditSelectedCommand = StatusContext.RunBlockingTaskCommand(EditSelected);
            DeleteSelectedCommand = StatusContext.RunBlockingTaskCommand(DeleteSelected);
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

        public Command EditSelectedCommand
        {
            get => _editSelectedCommand;
            set
            {
                if (Equals(value, _editSelectedCommand)) return;
                _editSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public NewContent NewActions
        {
            get => _newActions;
            set
            {
                if (Equals(value, _newActions)) return;
                _newActions = value;
                OnPropertyChanged();
            }
        }

        public async Task EditSelected()
        {
            if (ListSelection?.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
            {
                StatusContext.ToastWarning("Nothing Selected to Edit?");
                return;
            }

            if (ListSelection.SelectedItems.Count > 20)
            {
                StatusContext.ToastWarning("Sorry - please select less than 20 items to edit...");
                return;
            }

            var currentSelected = ListSelection.SelectedItems;

            foreach (var loopSelected in currentSelected)
            {
                switch (loopSelected)
                {
                    case FileListListItem x:
                        await x.ItemActions.Edit(x.DbEntry);
                        break;
                    case GeoJsonListListItem x:
                        await x.ItemActions.Edit(x.DbEntry);
                        break;
                    case ImageListListItem x:
                        await x.ItemActions.Edit(x.DbEntry);
                        break;
                    case LinkListListItem x:
                        await x.ItemActions.Edit(x.DbEntry);
                        break;
                    case LineListListItem x:
                        await x.ItemActions.Edit(x.DbEntry);
                        break;
                    case MapComponentListListItem x:
                        await x.ItemActions.Edit(x.DbEntry);
                        break;
                    case NoteListListItem x:
                        await x.ItemActions.Edit(x.DbEntry);
                        break;
                    case PointListListItem x:
                        await x.ItemActions.Edit(x.DbEntry);
                        break;
                    case PhotoListListItem x:
                        await x.ItemActions.Edit(x.DbEntry);
                        break;
                    case PostListListItem x:
                        await x.ItemActions.Edit(x.DbEntry);
                        break;
                }
            }
        }

        public async Task DeleteSelected()
        {
            if (ListSelection?.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
            {
                StatusContext.ToastWarning("Nothing Selected to Edit?");
                return;
            }

            if (ListSelection.SelectedItems.Count > 1)
                if (await StatusContext.ShowMessage("Delete Multiple Items",
                    $"You are about to delete {ListSelection.SelectedItems.Count} items - do you really want to delete all of these items?" +
                    $"{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, ListSelection.SelectedItems.Select(x => x.Content().Title))}",
                    new List<string> {"Yes", "No"}) == "No")
                    return;

            var currentSelected = ListSelection.SelectedItems;

            foreach (var loopSelected in currentSelected)
            {
                switch (loopSelected)
                {
                    case FileListListItem x:
                        await x.ItemActions.Delete(x.DbEntry);
                        break;
                    case GeoJsonListListItem x:
                        await x.ItemActions.Delete(x.DbEntry);
                        break;
                    case ImageListListItem x:
                        await x.ItemActions.Delete(x.DbEntry);
                        break;
                    case LinkListListItem x:
                        await x.ItemActions.Delete(x.DbEntry);
                        break;
                    case LineListListItem x:
                        await x.ItemActions.Delete(x.DbEntry);
                        break;
                    case MapComponentListListItem x:
                        await x.ItemActions.Delete(x.DbEntry);
                        break;
                    case NoteListListItem x:
                        await x.ItemActions.Delete(x.DbEntry);
                        break;
                    case PointListListItem x:
                        await x.ItemActions.Delete(x.DbEntry);
                        break;
                    case PhotoListListItem x:
                        await x.ItemActions.Delete(x.DbEntry);
                        break;
                    case PostListListItem x:
                        await x.ItemActions.Delete(x.DbEntry);
                        break;
                }
            }
        }

        public IContentListLoader ContentListLoader
        {
            get => _contentListLoader;
            set
            {
                if (Equals(value, _contentListLoader)) return;
                _contentListLoader = value;
                OnPropertyChanged();
            }
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

        public NoteListItemActions NoteItemActions
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

        private async Task DataNotificationReceived(TinyMessageReceivedEventArgs e)
        {
            var translatedMessage = DataNotifications.TranslateDataNotification(e.Message);

            if (translatedMessage.HasError)
            {
                Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}",
                    translatedMessage.ErrorNote, StatusContext.StatusControlContextId);
                return;
            }

            if (translatedMessage.ContentIds == null || !translatedMessage.ContentIds.Any()) return;

            var existingListItemsMatchingNotification = new List<IContentListItem>();

            foreach (var loopItem in Items)
            {
                var id = loopItem.ContentId();
                if (id == null) continue;
                if (translatedMessage.ContentIds.Contains(id.Value))
                    existingListItemsMatchingNotification.Add(loopItem);
            }

            if (ContentListLoader.DataNotificationTypesToRespondTo != null &&
                ContentListLoader.DataNotificationTypesToRespondTo.Any())
                if (!ContentListLoader.DataNotificationTypesToRespondTo.Contains(translatedMessage.ContentType))
                {
                    await PossibleMainImageUpdateDataNotificationReceived(translatedMessage);
                    return;
                }


            await ThreadSwitcher.ResumeBackgroundAsync();


            if (translatedMessage.UpdateType == DataNotificationUpdateType.Delete)
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                existingListItemsMatchingNotification.ForEach(x => Items.Remove(x));

                return;
            }

            var context = await Db.Context();
            var dbItems = new List<IContentId>();

            switch (translatedMessage.ContentType)
            {
                case DataNotificationContentType.File:
                    dbItems = (await context.FileContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                        .ToListAsync()).Cast<IContentId>().ToList();
                    break;
                case DataNotificationContentType.GeoJson:
                    dbItems = (await context.GeoJsonContents
                            .Where(x => translatedMessage.ContentIds.Contains(x.ContentId)).ToListAsync())
                        .Cast<IContentId>().ToList();
                    break;
                case DataNotificationContentType.Image:
                    dbItems = (await context.ImageContents
                            .Where(x => translatedMessage.ContentIds.Contains(x.ContentId)).ToListAsync())
                        .Cast<IContentId>().ToList();
                    break;
                case DataNotificationContentType.Line:
                    dbItems = (await context.LineContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                        .ToListAsync()).Cast<IContentId>().ToList();
                    break;
                case DataNotificationContentType.Link:
                    dbItems = (await context.LinkContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                        .ToListAsync()).Cast<IContentId>().ToList();
                    break;
                case DataNotificationContentType.Map:
                    dbItems = (await context.MapComponents
                            .Where(x => translatedMessage.ContentIds.Contains(x.ContentId)).ToListAsync())
                        .Cast<IContentId>().ToList();
                    break;
                case DataNotificationContentType.Note:
                    dbItems = (await context.NoteContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                        .ToListAsync()).Cast<IContentId>().ToList();
                    break;
                case DataNotificationContentType.Photo:
                    dbItems = (await context.PhotoContents
                            .Where(x => translatedMessage.ContentIds.Contains(x.ContentId)).ToListAsync())
                        .Cast<IContentId>().ToList();
                    break;
                case DataNotificationContentType.Point:
                    dbItems = (await context.PointContents
                            .Where(x => translatedMessage.ContentIds.Contains(x.ContentId)).ToListAsync())
                        .Cast<IContentId>().ToList();
                    break;
                case DataNotificationContentType.Post:
                    dbItems = (await context.PostContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                        .ToListAsync()).Cast<IContentId>().ToList();
                    break;
            }

            if (!dbItems.Any()) return;

            foreach (var loopItem in dbItems)
            {
                var existingItems = existingListItemsMatchingNotification
                    .Where(x => x.ContentId() == loopItem.ContentId).ToList();

                if (existingItems.Count > 1)
                {
                    await ThreadSwitcher.ResumeForegroundAsync();

                    foreach (var loopDelete in existingItems.Skip(1).ToList()) Items.Remove(loopDelete);

                    await ThreadSwitcher.ResumeBackgroundAsync();
                }

                var existingItem = existingItems.FirstOrDefault();

                if (existingItem == null)
                {
                    if (!ContentListLoader.AddNewItemsFromDataNotifications) continue;

                    await ThreadSwitcher.ResumeForegroundAsync();

                    Items.Add(ListItemFromDbItem(loopItem));

                    await ThreadSwitcher.ResumeBackgroundAsync();

                    continue;
                }

                if (translatedMessage.UpdateType == DataNotificationUpdateType.Update)
                    // ReSharper disable All
                    ((dynamic) existingItem).DbEntry = (dynamic) loopItem;
                // ReSharper restore All

                if (loopItem is IMainImage mainImage && existingItem is IContentListSmallImage itemWithSmallImage)
                    itemWithSmallImage.SmallImageUrl = GetSmallImageUrl(mainImage);
            }

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(FilterList);
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

        public static string GetSmallImageUrl(IMainImage content)
        {
            if (content?.MainPicture == null) return null;

            string smallImageUrl;

            try
            {
                smallImageUrl = PictureAssetProcessing.ProcessPictureDirectory(content.MainPicture.Value).SmallPicture
                    ?.File.FullName;
            }
            catch
            {
                smallImageUrl = null;
            }

            return smallImageUrl;
        }

        public IContentListItem ListItemFromDbItem(object dbItem)
        {
            return dbItem switch
            {
                FileContent f => FileListItemActions.ListItemFromDbItem(f, FileItemActions, ContentListLoader.ShowType),
                GeoJsonContent g => GeoJsonListItemActions.ListItemFromDbItem(g, GeoJasonItemActions,
                    ContentListLoader.ShowType),
                ImageContent g => ImageListItemActions.ListItemFromDbItem(g, ImageItemActions,
                    ContentListLoader.ShowType),
                LineContent l => LineListItemActions.ListItemFromDbItem(l, LineItemActions, ContentListLoader.ShowType),
                LinkContent k => LinkListItemActions.ListItemFromDbItem(k, LinkItemActions, ContentListLoader.ShowType),
                MapComponent m => MapComponentListItemActions.ListItemFromDbItem(m, MapComponentItemActions,
                    ContentListLoader.ShowType),
                NoteContent n => NoteListItemActions.ListItemFromDbItem(n, NoteItemActions, ContentListLoader.ShowType),
                PhotoContent ph => PhotoListItemActions.ListItemFromDbItem(ph, PhotoItemActions,
                    ContentListLoader.ShowType),
                PointContent pt => PointListItemActions.ListItemFromDbItem(pt, PointItemActions,
                    ContentListLoader.ShowType),
                PostContent po => PostListItemActions.ListItemFromDbItem(po, PostItemActions,
                    ContentListLoader.ShowType),
                _ => null
            };
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListSelection = await ContentListSelected<IContentListItem>.CreateInstance(StatusContext);

            DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

            StatusContext.Progress("Setting up Sorting");

            ListSort = new ColumnSortControlContext
            {
                Items = new List<ColumnSortControlSortItem>
                {
                    new()
                    {
                        DisplayName = "Updated",
                        ColumnName = "DbEntry.LatestUpdate",
                        Order = 1,
                        DefaultSortDirection = ListSortDirection.Descending
                    },
                    new()
                    {
                        DisplayName = "Created",
                        ColumnName = "DbEntry.CreatedOn",
                        DefaultSortDirection = ListSortDirection.Descending
                    },
                    new()
                    {
                        DisplayName = "Title",
                        ColumnName = "DbEntry.Title",
                        DefaultSortDirection = ListSortDirection.Ascending
                    }
                }
            };

            ListSort.SortUpdated += (sender, list) =>
                Dispatcher.CurrentDispatcher.Invoke(() => { ListContextSortHelpers.SortList(list, Items); });

            StatusContext.Progress("Starting Item Load");

            var dbItems = await ContentListLoader.LoadItems(StatusContext.ProgressTracker());

            StatusContext.Progress("Checking for All Items Loaded from Db");

            StatusContext.Progress($"All Items Loaded from Db: {ContentListLoader.AllItemsLoaded}");

            var contentListItems = new List<IContentListItem>();

            StatusContext.Progress("Creating List Items");

            var loopCounter = 0;
            foreach (var loopDbItem in dbItems)
            {
                if (loopCounter++ % 250 == 0)
                    StatusContext.Progress($"Created List Item {loopCounter} of {dbItems.Count}");
                contentListItems.Add(ListItemFromDbItem(loopDbItem));
            }

            contentListItems = dbItems.Select(ListItemFromDbItem).ToList();

            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Loading Display List of Items");

            Items = new ObservableCollection<IContentListItem>(contentListItems);

            ListContextSortHelpers.SortList(ListSort.SortDescriptions(), Items);
            await FilterList();

            DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
        }

        private void OnDataNotificationReceived(object sender, TinyMessageReceivedEventArgs e)
        {
            DataNotificationsProcessor.Enqueue(e);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task PossibleMainImageUpdateDataNotificationReceived(
            InterProcessDataNotification translatedMessage)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var smallImageListItems =
                Items.Where(x => x is IContentListSmallImage).Cast<IContentListSmallImage>().ToList();

            foreach (var loopListItem in smallImageListItems)
                if (((dynamic) loopListItem).DbEntry is IMainImage dbMainImageEntry &&
                    dbMainImageEntry.MainPicture != null &&
                    translatedMessage.ContentIds.Contains(dbMainImageEntry.MainPicture.Value))
                    loopListItem.SmallImageUrl = GetSmallImageUrl(dbMainImageEntry);
        }
    }
}