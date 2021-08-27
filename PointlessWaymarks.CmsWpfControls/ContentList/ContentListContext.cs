using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using GongSolutions.Wpf.DragDrop;
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
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.ContentList
{
    public class ContentListContext : INotifyPropertyChanged, IDragSource
    {
        private Command _bracketCodeToClipboardSelectedCommand;
        private IContentListLoader _contentListLoader;
        private List<ContextMenuItemData> _contextMenuItems;
        private Command _deleteSelectedCommand;
        private Command _editSelectedCommand;
        private FileContentActions _fileItemActions;
        private Command _generateHtmlSelectedCommand;
        private GeoJsonContentActions _geoJsonItemActions;
        private ImageContentActions _imageItemActions;
        private Command _importFromExcelFileCommand;
        private Command _importFromOpenExcelInstanceCommand;
        private ObservableCollection<IContentListItem> _items;
        private LineContentActions _lineItemActions;
        private LinkContentActions _linkItemActions;
        private ContentListSelected<IContentListItem> _listSelection;
        private ColumnSortControlContext _listSort;
        private Command _loadAllCommand;
        private MapComponentContentActions _mapComponentItemActions;
        private NewContent _newActions;
        private NoteContentActions _noteItemActions;
        private Command _openUrlSelectedCommand;
        private PhotoContentActions _photoItemActions;
        private PointContentActions _pointItemActions;
        private PostContentActions _postItemActions;
        private Command _selectedToExcelCommand;
        private StatusControlContext _statusContext;
        private string _userFilterText;
        private Command _viewHistorySelectedCommand;

        public ContentListContext(StatusControlContext statusContext, IContentListLoader loader)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            ContentListLoader = loader;

            FileItemActions = new FileContentActions(StatusContext);
            GeoJsonItemActions = new GeoJsonContentActions(StatusContext);
            ImageItemActions = new ImageContentActions(StatusContext);
            LineItemActions = new LineContentActions(StatusContext);
            LinkItemActions = new LinkContentActions(StatusContext);
            MapComponentItemActions = new MapComponentContentActions(StatusContext);
            NoteItemActions = new NoteContentActions(StatusContext);
            PointItemActions = new PointContentActions(StatusContext);
            PhotoItemActions = new PhotoContentActions(StatusContext);
            PostItemActions = new PostContentActions(StatusContext);

            NewActions = new NewContent(StatusContext);

            DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

            LoadAllCommand = StatusContext.RunBlockingTaskCommand(async () =>
            {
                ContentListLoader.PartialLoadQuantity = null;
                await LoadData();
            });

            DeleteSelectedCommand =
                StatusContext.RunBlockingTaskWithCancellationCommand(DeleteSelected, "Cancel Delete");
            BracketCodeToClipboardSelectedCommand =
                StatusContext.RunBlockingTaskWithCancellationCommand(BracketCodeToClipboardSelected, "Cancel Delete");
            EditSelectedCommand = StatusContext.RunBlockingTaskWithCancellationCommand(EditSelected, "Cancel Edit");
            ExtractNewLinksSelectedCommand =
                StatusContext.RunBlockingTaskWithCancellationCommand(ExtractNewLinksSelected, "Cancel Link Extraction");
            GenerateHtmlSelectedCommand =
                StatusContext.RunBlockingTaskWithCancellationCommand(GenerateHtmlSelected, "Cancel Generate Html");
            OpenUrlSelectedCommand =
                StatusContext.RunBlockingTaskWithCancellationCommand(OpenUrlSelected, "Cancel Open Url");
            ViewHistorySelectedCommand =
                StatusContext.RunBlockingTaskWithCancellationCommand(ViewHistorySelected, "Cancel View History");

            ImportFromExcelFileCommand =
                StatusContext.RunBlockingTaskCommand(async () => await ExcelHelpers.ImportFromExcelFile(StatusContext));
            ImportFromOpenExcelInstanceCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await ExcelHelpers.ImportFromOpenExcelInstance(StatusContext));
            SelectedToExcelCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
                await ExcelHelpers.SelectedToExcel(ListSelection.SelectedItems?.Cast<dynamic>().ToList(),
                    StatusContext));
        }

        public Command BracketCodeToClipboardSelectedCommand
        {
            get => _bracketCodeToClipboardSelectedCommand;
            set
            {
                if (Equals(value, _bracketCodeToClipboardSelectedCommand)) return;
                _bracketCodeToClipboardSelectedCommand = value;
                OnPropertyChanged();
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

        public List<ContextMenuItemData> ContextMenuItems
        {
            get => _contextMenuItems;
            set
            {
                if (Equals(value, _contextMenuItems)) return;
                _contextMenuItems = value;
                OnPropertyChanged();
            }
        }

        public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }

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

        public Command ExtractNewLinksSelectedCommand { get; set; }

        public FileContentActions FileItemActions
        {
            get => _fileItemActions;
            set
            {
                if (Equals(value, _fileItemActions)) return;
                _fileItemActions = value;
                OnPropertyChanged();
            }
        }

        public Command GenerateHtmlSelectedCommand
        {
            get => _generateHtmlSelectedCommand;
            set
            {
                if (Equals(value, _generateHtmlSelectedCommand)) return;
                _generateHtmlSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public GeoJsonContentActions GeoJsonItemActions
        {
            get => _geoJsonItemActions;
            set
            {
                if (Equals(value, _geoJsonItemActions)) return;
                _geoJsonItemActions = value;
                OnPropertyChanged();
            }
        }

        public ImageContentActions ImageItemActions
        {
            get => _imageItemActions;
            set
            {
                if (Equals(value, _imageItemActions)) return;
                _imageItemActions = value;
                OnPropertyChanged();
            }
        }

        public Command ImportFromExcelFileCommand
        {
            get => _importFromExcelFileCommand;
            set
            {
                if (Equals(value, _importFromExcelFileCommand)) return;
                _importFromExcelFileCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ImportFromOpenExcelInstanceCommand
        {
            get => _importFromOpenExcelInstanceCommand;
            set
            {
                if (Equals(value, _importFromOpenExcelInstanceCommand)) return;
                _importFromOpenExcelInstanceCommand = value;
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

        public LineContentActions LineItemActions
        {
            get => _lineItemActions;
            set
            {
                if (Equals(value, _lineItemActions)) return;
                _lineItemActions = value;
                OnPropertyChanged();
            }
        }

        public LinkContentActions LinkItemActions
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

        public MapComponentContentActions MapComponentItemActions
        {
            get => _mapComponentItemActions;
            set
            {
                if (Equals(value, _mapComponentItemActions)) return;
                _mapComponentItemActions = value;
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

        public NoteContentActions NoteItemActions
        {
            get => _noteItemActions;
            set
            {
                if (Equals(value, _noteItemActions)) return;
                _noteItemActions = value;
                OnPropertyChanged();
            }
        }

        public Command OpenUrlSelectedCommand
        {
            get => _openUrlSelectedCommand;
            set
            {
                if (Equals(value, _openUrlSelectedCommand)) return;
                _openUrlSelectedCommand = value;
                OnPropertyChanged();
            }
        }


        public PhotoContentActions PhotoItemActions
        {
            get => _photoItemActions;
            set
            {
                if (Equals(value, _photoItemActions)) return;
                _photoItemActions = value;
                OnPropertyChanged();
            }
        }

        public PointContentActions PointItemActions
        {
            get => _pointItemActions;
            set
            {
                if (Equals(value, _pointItemActions)) return;
                _pointItemActions = value;
                OnPropertyChanged();
            }
        }

        public PostContentActions PostItemActions
        {
            get => _postItemActions;
            set
            {
                if (Equals(value, _postItemActions)) return;
                _postItemActions = value;
                OnPropertyChanged();
            }
        }

        public Command SelectedToExcelCommand
        {
            get => _selectedToExcelCommand;
            set
            {
                if (Equals(value, _selectedToExcelCommand)) return;
                _selectedToExcelCommand = value;
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

                StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
            }
        }


        public Command ViewHistorySelectedCommand
        {
            get => _viewHistorySelectedCommand;
            set
            {
                if (Equals(value, _viewHistorySelectedCommand)) return;
                _viewHistorySelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public bool CanStartDrag(IDragInfo dragInfo)
        {
            return (ListSelection.SelectedItems?.Count ?? 0) > 0;
        }

        public void DragCancelled()
        {
        }

        public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
        {
        }

        public void Dropped(IDropInfo dropInfo)
        {
        }

        public void StartDrag(IDragInfo dragInfo)
        {
            var defaultBracketCodeList = ListSelection.SelectedItems.Select(x => x.DefaultBracketCode()).ToList();
            dragInfo.Data = string.Join(Environment.NewLine, defaultBracketCodeList);
            dragInfo.DataFormat = DataFormats.GetDataFormat(DataFormats.UnicodeText);
            dragInfo.Effects = DragDropEffects.Copy;
        }

        public bool TryCatchOccurredException(Exception exception)
        {
            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task BracketCodeToClipboardSelected(CancellationToken cancelToken)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListSelection?.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
            {
                StatusContext.ToastWarning("Nothing Selected to Edit?");
                return;
            }

            var currentSelected = ListSelection.SelectedItems;

            var bracketCodes = new List<string>();

            foreach (var loopSelected in currentSelected)
            {
                cancelToken.ThrowIfCancellationRequested();

                bracketCodes.Add(loopSelected.DefaultBracketCode());
            }

            var finalString = string.Join(Environment.NewLine, bracketCodes.Where(x => !string.IsNullOrWhiteSpace(x)));

            if (string.IsNullOrWhiteSpace(finalString))
            {
                StatusContext.ToastSuccess("No Bracket Codes Found?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess("Bracket Codes copied to Clipboard");
        }

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
                var id = loopItem?.ContentId();
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
                    ((dynamic)existingItem).DbEntry = (dynamic)loopItem;
                // ReSharper restore All

                if (loopItem is IMainImage mainImage && existingItem is IContentListSmallImage itemWithSmallImage)
                    itemWithSmallImage.SmallImageUrl = GetSmallImageUrl(mainImage);
            }

            StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
        }

        public async Task DeleteSelected(CancellationToken cancelToken)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListSelection?.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
            {
                StatusContext.ToastWarning("Nothing Selected to Edit?");
                return;
            }

            if (ListSelection.SelectedItems.Count > 20)
                if (await StatusContext.ShowMessage("Delete Multiple Items",
                    $"You are about to delete {ListSelection.SelectedItems.Count} items - do you really want to delete all of these items?" +
                    $"{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, ListSelection.SelectedItems.Select(x => x.Content().Title))}",
                    new List<string> { "Yes", "No" }) == "No")
                    return;

            var currentSelected = ListSelection.SelectedItems;

            foreach (var loopSelected in currentSelected)
            {
                cancelToken.ThrowIfCancellationRequested();

                await loopSelected.Delete();
            }
        }

        public async Task EditSelected(CancellationToken cancelToken)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

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
                cancelToken.ThrowIfCancellationRequested();

                await loopSelected.Edit();
            }
        }

        public async Task ExtractNewLinksSelected(CancellationToken cancelToken)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListSelection?.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
            {
                StatusContext.ToastWarning("Nothing Selected to Edit?");
                return;
            }

            var currentSelected = ListSelection.SelectedItems;

            foreach (var loopSelected in currentSelected)
            {
                cancelToken.ThrowIfCancellationRequested();

                await loopSelected.ExtractNewLinks();
            }
        }

        private async Task FilterList()
        {
            if (Items == null || !Items.Any()) return;

            await ThreadSwitcher.ResumeForegroundAsync();

            ((CollectionView)CollectionViewSource.GetDefaultView(Items)).Filter = o =>
            {
                if (string.IsNullOrWhiteSpace(UserFilterText)) return true;

                if (o is not IContentListItem toFilter) return false;

                if ((toFilter.Content().Title ?? string.Empty).Contains(UserFilterText,
                    StringComparison.OrdinalIgnoreCase)) return true;
                if ((toFilter.Content().Tags ?? string.Empty).Contains(UserFilterText,
                    StringComparison.OrdinalIgnoreCase)) return true;
                if ((toFilter.Content().Summary ?? string.Empty).Contains(UserFilterText,
                    StringComparison.OrdinalIgnoreCase)) return true;
                if ((toFilter.Content().CreatedBy ?? string.Empty).Contains(UserFilterText,
                    StringComparison.OrdinalIgnoreCase)) return true;
                if ((toFilter.Content().LastUpdatedBy ?? string.Empty).Contains(UserFilterText,
                    StringComparison.OrdinalIgnoreCase)) return true;
                if (toFilter.ContentId() != null && toFilter.ContentId().ToString()
                    .Contains(UserFilterText, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            };
        }

        public async Task GenerateHtmlSelected(CancellationToken cancelToken)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListSelection?.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
            {
                StatusContext.ToastWarning("Nothing Selected to Generate?");
                return;
            }

            var currentSelected = ListSelection.SelectedItems;

            foreach (var loopSelected in currentSelected)
            {
                cancelToken.ThrowIfCancellationRequested();

                await loopSelected.GenerateHtml();
            }
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
                FileContent f => FileContentActions.ListItemFromDbItem(f, FileItemActions, ContentListLoader.ShowType),
                GeoJsonContent g => GeoJsonContentActions.ListItemFromDbItem(g, GeoJsonItemActions,
                    ContentListLoader.ShowType),
                ImageContent g => ImageContentActions.ListItemFromDbItem(g, ImageItemActions,
                    ContentListLoader.ShowType),
                LineContent l => LineContentActions.ListItemFromDbItem(l, LineItemActions, ContentListLoader.ShowType),
                LinkContent k => LinkContentActions.ListItemFromDbItem(k, LinkItemActions, ContentListLoader.ShowType),
                MapComponent m => MapComponentContentActions.ListItemFromDbItem(m, MapComponentItemActions,
                    ContentListLoader.ShowType),
                NoteContent n => NoteContentActions.ListItemFromDbItem(n, NoteItemActions, ContentListLoader.ShowType),
                PhotoContent ph => PhotoContentActions.ListItemFromDbItem(ph, PhotoItemActions,
                    ContentListLoader.ShowType),
                PointContent pt => PointContentActions.ListItemFromDbItem(pt, PointItemActions,
                    ContentListLoader.ShowType),
                PostContent po => PostContentActions.ListItemFromDbItem(po, PostItemActions,
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

            ListSort = ContentListLoader.SortContext();

            ListSort.SortUpdated += (_, list) =>
                Dispatcher.CurrentDispatcher.Invoke(() => { ListContextSortHelpers.SortList(list, Items); });

            StatusContext.Progress("Starting Item Load");

            var dbItems = await ContentListLoader.LoadItems(StatusContext.ProgressTracker());

            StatusContext.Progress($"All Items Loaded from Db: {ContentListLoader.AllItemsLoaded}");

            var contentListItems = new List<IContentListItem>();

            StatusContext.Progress("Creating List Items");

            var loopCounter = 0;

            Parallel.ForEach(dbItems, loopDbItem =>
            {
                Interlocked.Increment(ref loopCounter);

                if (loopCounter % 250 == 0)
                    StatusContext.Progress($"Created List Item {loopCounter} of {dbItems.Count}");

                contentListItems.Add(ListItemFromDbItem(loopDbItem));
            });

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

        public async Task OpenUrlSelected(CancellationToken cancelToken)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListSelection?.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
            {
                StatusContext.ToastWarning("Nothing Selected to Edit?");
                return;
            }

            var currentSelected = ListSelection.SelectedItems;

            foreach (var loopSelected in currentSelected) await loopSelected.OpenUrl();
        }

        private async Task PossibleMainImageUpdateDataNotificationReceived(
            InterProcessDataNotification translatedMessage)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var smallImageListItems =
                Items.Where(x => x is IContentListSmallImage).Cast<IContentListSmallImage>().ToList();

            foreach (var loopListItem in smallImageListItems)
                if (((dynamic)loopListItem).DbEntry is IMainImage { MainPicture: { } } dbMainImageEntry &&
                    translatedMessage.ContentIds.Contains(dbMainImageEntry.MainPicture.Value))
                    loopListItem.SmallImageUrl = GetSmallImageUrl(dbMainImageEntry);
        }

        public async Task ViewHistorySelected(CancellationToken cancelToken)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListSelection?.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
            {
                StatusContext.ToastWarning("Nothing Selected to Edit?");
                return;
            }

            var currentSelected = ListSelection.SelectedItems;

            foreach (var loopSelected in currentSelected)
            {
                cancelToken.ThrowIfCancellationRequested();
                await loopSelected.ViewHistory();
            }
        }
    }
}