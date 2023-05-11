using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.AllContentList;
using PointlessWaymarks.CmsWpfControls.ColumnSort;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.ImageContentEditor;
using PointlessWaymarks.CmsWpfControls.ImageList;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.LinkList;
using PointlessWaymarks.CmsWpfControls.MapComponentList;
using PointlessWaymarks.CmsWpfControls.NoteList;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CmsWpfControls.PostList;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.Excel;
using PointlessWaymarks.CmsWpfControls.VideoContentEditor;
using PointlessWaymarks.CmsWpfControls.VideoList;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

public partial class ContentListContext : ObservableObject, IDragSource, IDropTarget
{
    [ObservableProperty] private RelayCommand _bracketCodeToClipboardSelectedCommand;
    [ObservableProperty] private IContentListLoader _contentListLoader;
    [ObservableProperty] private List<ContextMenuItemData> _contextMenuItems = new();
    [ObservableProperty] private RelayCommand<DateTime?> _createdOnDaySearchCommand;
    [ObservableProperty] private DataNotificationsWorkQueue _dataNotificationsProcessor;
    [ObservableProperty] private RelayCommand _deleteSelectedCommand;
    [ObservableProperty] private RelayCommand _editSelectedCommand;
    [ObservableProperty] private RelayCommand _extractNewLinksSelectedCommand;
    [ObservableProperty] private FileContentActions _fileItemActions;
    [ObservableProperty] private bool _filterOnUiShown;
    [ObservableProperty] private RelayCommand<string> _folderSearchCommand;
    [ObservableProperty] private RelayCommand _generateHtmlSelectedCommand;
    [ObservableProperty] private GeoJsonContentActions _geoJsonItemActions;
    [ObservableProperty] private ImageContentActions _imageItemActions;
    [ObservableProperty] private RelayCommand _importFromExcelFileCommand;
    [ObservableProperty] private RelayCommand _importFromOpenExcelInstanceCommand;
    [ObservableProperty] private ObservableCollection<IContentListItem> _items;
    [ObservableProperty] private RelayCommand<DateTime?> _lastUpdatedOnDaySearchCommand;
    [ObservableProperty] private LineContentActions _lineItemActions;
    [ObservableProperty] private LinkContentActions _linkItemActions;
    [ObservableProperty] private ContentListSelected<IContentListItem> _listSelection;
    [ObservableProperty] private ColumnSortControlContext _listSort;
    [ObservableProperty] private RelayCommand _loadAllCommand;
    [ObservableProperty] private MapComponentContentActions _mapComponentItemActions;
    [ObservableProperty] private CmsCommonCommands _newActions;
    [ObservableProperty] private NoteContentActions _noteItemActions;
    [ObservableProperty] private PhotoContentActions _photoItemActions;
    [ObservableProperty] private PointContentActions _pointItemActions;
    [ObservableProperty] private PostContentActions _postItemActions;
    [ObservableProperty] private RelayCommand _selectedToExcelCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string? _userFilterText;
    [ObservableProperty] private VideoContentActions _videoItemActions;
    [ObservableProperty] private RelayCommand _viewHistorySelectedCommand;
    [ObservableProperty] private RelayCommand _viewOnSiteCommand;
    [ObservableProperty] private WindowIconStatus? _windowStatus;

    private ContentListContext(StatusControlContext? statusContext,
        ObservableCollection<IContentListItem> factoryContentListItems,
        ContentListSelected<IContentListItem> factoryListSelection, IContentListLoader loader,
        WindowIconStatus? windowStatus = null)
    {
        _statusContext = statusContext ?? new StatusControlContext();

        StatusContext.PropertyChanged += StatusContextOnPropertyChanged;

        _windowStatus = windowStatus;

        PropertyChanged += OnPropertyChanged;

        _contentListLoader = loader;

        _items = factoryContentListItems;
        _listSelection = factoryListSelection;

        _fileItemActions = new FileContentActions(StatusContext);
        _geoJsonItemActions = new GeoJsonContentActions(StatusContext);
        _imageItemActions = new ImageContentActions(StatusContext);
        _lineItemActions = new LineContentActions(StatusContext);
        _linkItemActions = new LinkContentActions(StatusContext);
        _mapComponentItemActions = new MapComponentContentActions(StatusContext);
        _noteItemActions = new NoteContentActions(StatusContext);
        _pointItemActions = new PointContentActions(StatusContext);
        _photoItemActions = new PhotoContentActions(StatusContext);
        _postItemActions = new PostContentActions(StatusContext);
        _videoItemActions = new VideoContentActions(StatusContext);

        _newActions = new CmsCommonCommands(StatusContext, WindowStatus);

        _dataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        _listSort = ContentListLoader.SortContext();

        _listSort.SortUpdated += (_, list) =>
            Dispatcher.CurrentDispatcher.Invoke(() => { ListContextSortHelpers.SortList(list, Items); });

        _loadAllCommand = StatusContext.RunBlockingTaskCommand(async () =>
        {
            ContentListLoader.PartialLoadQuantity = null;
            await LoadData();
        });

        _deleteSelectedCommand = StatusContext.RunBlockingTaskWithCancellationCommand(DeleteSelected, "Cancel Delete");
        _bracketCodeToClipboardSelectedCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(BracketCodeToClipboardSelected, "Cancel Delete");
        _editSelectedCommand = StatusContext.RunBlockingTaskWithCancellationCommand(EditSelected, "Cancel Edit");
        _extractNewLinksSelectedCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(ExtractNewLinksSelected, "Cancel Link Extraction");
        _generateHtmlSelectedCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(GenerateHtmlSelected, "Cancel Generate Html");
        _viewOnSiteCommand = StatusContext.RunBlockingTaskWithCancellationCommand(ViewOnSiteSelected, "Cancel Open Url");
        _viewHistorySelectedCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(ViewHistorySelected, "Cancel View History");

        _importFromExcelFileCommand =
            StatusContext.RunBlockingTaskCommand(async () => await ExcelHelpers.ImportFromExcelFile(StatusContext));
        _importFromOpenExcelInstanceCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await ExcelHelpers.ImportFromOpenExcelInstance(StatusContext));
        _selectedToExcelCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
            await ExcelHelpers.SelectedToExcel(ListSelection.SelectedItems?.Cast<dynamic>().ToList(), StatusContext));

        _folderSearchCommand = StatusContext.RunNonBlockingTaskCommand<string>(async x =>
            await RunReport(async () => await FolderSearch(x), $"Folder Search - {x}"));
        _createdOnDaySearchCommand = StatusContext.RunNonBlockingTaskCommand<DateTime?>(async x =>
            await RunReport(async () => await CreatedOnDaySearch(x), $"Created On Search - {x}"));
        _lastUpdatedOnDaySearchCommand = StatusContext.RunNonBlockingTaskCommand<DateTime?>(async x =>
            await RunReport(async () => await UpdatedOnDaySearch(x), $"Last Updated On Search - {x}"));
    }

    public static async Task<ContentListContext> CreateInstance(StatusControlContext? statusContext, IContentListLoader loader, WindowIconStatus? windowStatus = null)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var factoryObservable = new ObservableCollection<IContentListItem>();

        await ThreadSwitcher.ResumeBackgroundAsync();
        var factoryContext = statusContext ?? new StatusControlContext();
        var factoryListSelection = await ContentListSelected<IContentListItem>.CreateInstance(factoryContext);

        return new ContentListContext(statusContext, factoryObservable, factoryListSelection, loader, windowStatus);
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
        var defaultBracketCodeList = ListSelection.SelectedItems?.Select(x => x.DefaultBracketCode()).ToList();
        if (defaultBracketCodeList == null) return;
        dragInfo.Data = string.Join(Environment.NewLine, defaultBracketCodeList);
        dragInfo.DataFormat = DataFormats.GetDataFormat(DataFormats.UnicodeText);
        dragInfo.Effects = DragDropEffects.Copy;
    }

    public bool TryCatchOccurredException(Exception exception)
    {
        return false;
    }

    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not IDataObject systemDataObject ||
            !systemDataObject.GetDataPresent(DataFormats.FileDrop)) return;

        if (systemDataObject.GetData(DataFormats.FileDrop) is not string[] possibleFileInfo ||
            !possibleFileInfo.Any()) return;

        var validFileExtensions = new List<string>
        {
            ".PDF",
            ".MPG",
            ".MPEG",
            ".WAV",
            ".JPG",
            ".JPEG",
            ".GPX",
            ".TCX",
            ".FIT",
            "MP4",
            "OGG",
            "WEBM"
        };

        if (possibleFileInfo.Any(x => validFileExtensions.Contains(Path.GetExtension(x).ToUpperInvariant())))
            dropInfo.Effects = DragDropEffects.Link;
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is IDataObject systemDataObject && systemDataObject.GetDataPresent(DataFormats.FileDrop))
        {
            if (systemDataObject.GetData(DataFormats.FileDrop) is not string[] possibleFileInfo)
            {
                StatusContext.ToastError("Couldn't understand the dropped files?");
                return;
            }

            StatusContext.RunBlockingTask(async () =>
                await TryOpenEditorsForDroppedFiles(possibleFileInfo.ToList(), StatusContext));
        }
    }

    public async Task BracketCodeToClipboardSelected(CancellationToken cancelToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (ListSelection.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
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

    public static async Task<List<object>> CreatedOnDaySearch(DateTime? createdOn)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (createdOn == null) return new List<object>();

        return (await Db.ContentCreatedOnDay(createdOn.Value)).ToList();
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

        if (!translatedMessage.ContentIds.Any()) return;

        var existingListItemsMatchingNotification = new List<IContentListItem>();

        foreach (var loopItem in Items)
        {
            var id = loopItem.ContentId();
            if (id == null) continue;
            if (translatedMessage.ContentIds.Contains(id.Value))
                existingListItemsMatchingNotification.Add(loopItem);
        }

        if (ContentListLoader.DataNotificationTypesToRespondTo.Any())
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

        //!!Content List
        switch (translatedMessage.ContentType)
        {
            case DataNotificationContentType.File:
                dbItems = (await context.FileContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).Cast<IContentId>().ToList();
                break;
            case DataNotificationContentType.GeoJson:
                dbItems = (await context.GeoJsonContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).Cast<IContentId>().ToList();
                break;
            case DataNotificationContentType.Image:
                dbItems = (await context.ImageContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).Cast<IContentId>().ToList();
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
                dbItems = (await context.MapComponents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).Cast<IContentId>().ToList();
                break;
            case DataNotificationContentType.Note:
                dbItems = (await context.NoteContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).Cast<IContentId>().ToList();
                break;
            case DataNotificationContentType.Photo:
                dbItems = (await context.PhotoContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).Cast<IContentId>().ToList();
                break;
            case DataNotificationContentType.Point:
                dbItems = (await context.PointContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).Cast<IContentId>().ToList();
                break;
            case DataNotificationContentType.Post:
                dbItems = (await context.PostContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).Cast<IContentId>().ToList();
                break;
            case DataNotificationContentType.Video:
                dbItems = (await context.VideoContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).Cast<IContentId>().ToList();
                break;
        }

        if (!dbItems.Any()) return;

        foreach (var loopItem in dbItems)
        {
            var existingItems = existingListItemsMatchingNotification.Where(x => x.ContentId() == loopItem.ContentId)
                .ToList();

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

                var listItem = await ListItemFromDbItem(loopItem);

                if (listItem == null) continue;

                Items.Add(listItem);

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

        if (StatusContext.BlockUi) FilterOnUiShown = true;
        else StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }

    public async Task DeleteSelected(CancellationToken cancelToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (ListSelection.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
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

        if (ListSelection.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
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

        if (ListSelection.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
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
        if (!Items.Any()) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        if (string.IsNullOrWhiteSpace(UserFilterText))
        {
            ((CollectionView)CollectionViewSource.GetDefaultView(Items)).Filter = _ => true;
            return;
        }

        var searchFilterStack =
            new List<(Func<IContentListItem, string, Func<bool, bool>, ContentListSearch.ContentListSearchReturn> filter
                , string searchString, Func<bool, bool> searchModifier)>();

        using var sr = new StringReader(UserFilterText);

        while (await sr.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var searchString = line.Trim();
            Func<bool, bool> searchResultModifier = x => x;

            if (line.ToUpper().StartsWith("!"))
            {
                searchResultModifier = x => !x;
                searchString = line[1..];
                searchString = searchString.Trim();
            }

            if (string.IsNullOrWhiteSpace(searchString)) continue;

            Func<IContentListItem, string, Func<bool, bool>, ContentListSearch.ContentListSearchReturn>
                searchFilterFunction;

            if (searchString.ToUpper().StartsWith("SUMMARY:")) searchFilterFunction = ContentListSearch.SearchSummary;
            else if (searchString.ToUpper().StartsWith("TITLE:")) searchFilterFunction = ContentListSearch.SearchTitle;
            else if (searchString.ToUpper().StartsWith("FOLDER:"))
                searchFilterFunction = ContentListSearch.SearchFolder;
            else if (searchString.ToUpper().StartsWith("CREATED ON:"))
                searchFilterFunction = ContentListSearch.SearchCreatedOn;
            else if (searchString.ToUpper().StartsWith("CREATED BY:"))
                searchFilterFunction = ContentListSearch.SearchCreatedBy;
            else if (searchString.ToUpper().StartsWith("LAST UPDATED ON:"))
                searchFilterFunction = ContentListSearch.SearchLastUpdatedOn;
            else if (searchString.ToUpper().StartsWith("LAST UPDATED BY:"))
                searchFilterFunction = ContentListSearch.SearchLastUpdatedBy;
            else if (searchString.ToUpper().StartsWith("TAGS:")) searchFilterFunction = ContentListSearch.SearchTags;
            else if (searchString.ToUpper().StartsWith("CAMERA:"))
                searchFilterFunction = ContentListSearch.SearchCamera;
            else if (searchString.ToUpper().StartsWith("LENS:")) searchFilterFunction = ContentListSearch.SearchLens;
            else if (searchString.ToUpper().StartsWith("LICENSE:"))
                searchFilterFunction = ContentListSearch.SearchLicense;
            else if (searchString.ToUpper().StartsWith("PHOTO CREATED ON:"))
                searchFilterFunction = ContentListSearch.SearchPhotoCreatedOn;
            else if (searchString.ToUpper().StartsWith("APERTURE:"))
                searchFilterFunction = ContentListSearch.SearchAperture;
            else if (searchString.ToUpper().StartsWith("SHUTTER SPEED:"))
                searchFilterFunction = ContentListSearch.SearchShutterSpeed;
            else if (searchString.ToUpper().StartsWith("ISO:")) searchFilterFunction = ContentListSearch.SearchIso;
            else if (searchString.ToUpper().StartsWith("FOCAL LENGTH:"))
                searchFilterFunction = ContentListSearch.SearchFocalLength;
            else searchFilterFunction = ContentListSearch.SearchGeneral;

            searchFilterStack.Add((searchFilterFunction, searchString, searchResultModifier));
        }

        if (!searchFilterStack.Any())
        {
            ((CollectionView)CollectionViewSource.GetDefaultView(Items)).Filter = _ => true;
            return;
        }

        ((CollectionView)CollectionViewSource.GetDefaultView(Items)).Filter = o =>
        {
            if (o is not IContentListItem toFilter) return false;

            var filterResults = searchFilterStack.Select(x => x.filter(toFilter, x.searchString, x.searchModifier))
                .ToList();

            return !filterResults.Any() || filterResults.All(x => x.ResultModifier(x.SearchFunctionReturn.Include));
        };
    }

    public static async Task<List<object>> FolderSearch(string? folderName)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        return (await Db.ContentInFolder(folderName ?? string.Empty)).ToList();
    }

 

    public async Task GenerateHtmlSelected(CancellationToken cancelToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (ListSelection.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
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

    public static string? GetSmallImageUrl(IMainImage? content)
    {
        if (content?.MainPicture == null) return null;

        string? smallImageUrl;

        try
        {
            smallImageUrl = PictureAssetProcessing.ProcessPictureDirectory(content.MainPicture.Value)?.SmallPicture
                ?.File?.FullName;
        }
        catch
        {
            smallImageUrl = null;
        }

        return smallImageUrl;
    }

    public async Task<IContentListItem?> ListItemFromDbItem(object? dbItem)
    {
        //!!Content List
        return dbItem switch
        {
            FileContent f => await FileContentActions.ListItemFromDbItem(f, FileItemActions, ContentListLoader.ShowType),
            GeoJsonContent g => await GeoJsonContentActions.ListItemFromDbItem(g, GeoJsonItemActions,
                ContentListLoader.ShowType),
            ImageContent g => await ImageContentActions.ListItemFromDbItem(g, ImageItemActions, ContentListLoader.ShowType),
            LineContent l => await LineContentActions.ListItemFromDbItem(l, LineItemActions, ContentListLoader.ShowType),
            LinkContent k => await LinkContentActions.ListItemFromDbItem(k, LinkItemActions, ContentListLoader.ShowType),
            MapComponent m => await MapComponentContentActions.ListItemFromDbItem(m, MapComponentItemActions,
                ContentListLoader.ShowType),
            NoteContent n => await NoteContentActions.ListItemFromDbItem(n, NoteItemActions, ContentListLoader.ShowType),
            PhotoContent ph => await PhotoContentActions.ListItemFromDbItem(ph, PhotoItemActions, ContentListLoader.ShowType),
            PointContent pt => await PointContentActions.ListItemFromDbItem(pt, PointItemActions, ContentListLoader.ShowType),
            PostContent po => await PostContentActions.ListItemFromDbItem(po, PostItemActions, ContentListLoader.ShowType),
            VideoContent v => await VideoContentActions.ListItemFromDbItem(v, VideoItemActions, ContentListLoader.ShowType),
            _ => null
        };
    }

    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

        StatusContext.Progress("Starting Item Load");

        var dbItems = await ContentListLoader.LoadItems(StatusContext.ProgressTracker());

        StatusContext.Progress($"All Items Loaded from Db: {ContentListLoader.AllItemsLoaded}");

        var contentListItems = new ConcurrentBag<IContentListItem>();

        StatusContext.Progress("Creating List Items");

        var loopCounter = 0;

        await Parallel.ForEachAsync(dbItems,async (loopDbItem , _) =>
        {
            Interlocked.Increment(ref loopCounter);

            if (loopCounter % 250 == 0)
                StatusContext.Progress($"Created List Item {loopCounter} of {dbItems.Count}");

            var listItem = await ListItemFromDbItem(loopDbItem);

            if(listItem == null) return;

            contentListItems.Add(listItem);
        });

        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Loading Display List of Items");

        Items = new ObservableCollection<IContentListItem>(contentListItems);

        ListContextSortHelpers.SortList(ListSort.SortDescriptions(), Items);
        await FilterList();

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor.Enqueue(e);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(UserFilterText))
            StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }

    private async Task PossibleMainImageUpdateDataNotificationReceived(InterProcessDataNotification? translatedMessage)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (translatedMessage?.ContentIds == null) return;

        var smallImageListItems = Items.Where(x => x is IContentListSmallImage).Cast<IContentListSmallImage>().ToList();

        foreach (var loopListItem in smallImageListItems)
            if (((dynamic)loopListItem).DbEntry is IMainImage { MainPicture: { } } dbMainImageEntry &&
                translatedMessage.ContentIds.Contains(dbMainImageEntry.MainPicture.Value))
                loopListItem.SmallImageUrl = GetSmallImageUrl(dbMainImageEntry);
    }

    public static async Task RunReport(Func<Task<List<object>>> toRun, string title)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var reportLoader = new ContentListLoaderReport(toRun, ContentListLoaderBase.SortContextDefault());

        var newWindow =
            await AllContentListWindow.CreateInstance(await AllContentListWithActionsContext.CreateInstance(null, reportLoader));
        newWindow.WindowTitle = title;

        await newWindow.PositionWindowAndShowOnUiThread();
    }

    private void StatusContextOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        if (e.PropertyName == nameof(StatusContext.BlockUi) && !StatusContext.BlockUi && FilterOnUiShown)
        {
            FilterOnUiShown = !FilterOnUiShown;
            StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
        }
    }

    private async Task TryOpenEditorsForDroppedFiles(List<string> files, StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var fileContentExtensions = new List<string> { ".PDF", ".MPG", ".MPEG", ".WAV" };
        var pictureContentExtensions = new List<string> { ".JPG", ".JPEG" };
        var lineContentExtensions = new List<string> { ".GPX", ".TCX", ".FIT" };
        var videoContentExtensions = new List<string> { ".MP4", ".OGG", ".WEBM" };

        foreach (var loopFile in files)
        {
            var fileInfo = new FileInfo(loopFile);

            if (!fileInfo.Exists)
            {
                StatusContext.ToastError($"File {loopFile} doesn't exist?");
                continue;
            }

            if (fileContentExtensions.Contains(Path.GetExtension(loopFile).ToUpperInvariant()))
            {
                var newEditor = await FileContentEditorWindow.CreateInstance(new FileInfo(loopFile));
                await newEditor.PositionWindowAndShowOnUiThread();

                statusContext.ToastSuccess($"{Path.GetFileName(loopFile)} sent to File Editor");

                continue;
            }

            if (lineContentExtensions.Contains(Path.GetExtension(loopFile).ToUpperInvariant()))
            {
                await CmsCommonCommands.NewLineContentFromFiles(fileInfo.AsList(), false, CancellationToken.None,
                    StatusContext,
                    WindowStatus);
                continue;
            }

            if (pictureContentExtensions.Contains(Path.GetExtension(loopFile).ToUpperInvariant()))
            {
                string make;
                string model;

                try
                {
                    var exifDirectory = ImageMetadataReader.ReadMetadata(loopFile).OfType<ExifIfd0Directory>()
                        .FirstOrDefault();

                    make = exifDirectory?.GetDescription(ExifDirectoryBase.TagMake) ?? string.Empty;
                    model = exifDirectory?.GetDescription(ExifDirectoryBase.TagModel) ?? string.Empty;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }


                if (!string.IsNullOrWhiteSpace(make) || !string.IsNullOrWhiteSpace(model))
                {
                    var photoEditorWindow = await PhotoContentEditorWindow.CreateInstance(new FileInfo(loopFile));
                    await photoEditorWindow.PositionWindowAndShowOnUiThread();

                    statusContext.ToastSuccess($"{Path.GetFileName(loopFile)} sent to Photo Editor");
                }
                else
                {
                    var imageEditorWindow = await ImageContentEditorWindow.CreateInstance(null, new FileInfo(loopFile));
                    await imageEditorWindow.PositionWindowAndShowOnUiThread();

                    statusContext.ToastSuccess($"{Path.GetFileName(loopFile)} sent to Image Editor");
                }
            }

            if (videoContentExtensions.Contains(Path.GetExtension(loopFile).ToUpperInvariant()))
            {
                var newEditor = await VideoContentEditorWindow.CreateInstance(new FileInfo(loopFile));
                await newEditor.PositionWindowAndShowOnUiThread();

                statusContext.ToastSuccess($"{Path.GetFileName(loopFile)} sent to Video Editor");
            }
        }
    }

    public static async Task<List<object>> UpdatedOnDaySearch(DateTime? createdOn)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        return createdOn == null
            ? (await Db.ContentNeverUpdated()).ToList()
            : (await Db.ContentUpdatedOnDay(createdOn.Value)).ToList();
    }

    public async Task ViewHistorySelected(CancellationToken cancelToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (ListSelection.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
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

    public async Task ViewOnSiteSelected(CancellationToken cancelToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (ListSelection.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
        {
            StatusContext.ToastWarning("Nothing Selected to Edit?");
            return;
        }

        var currentSelected = ListSelection.SelectedItems;

        foreach (var loopSelected in currentSelected) await loopSelected.ViewOnSite();
    }
}