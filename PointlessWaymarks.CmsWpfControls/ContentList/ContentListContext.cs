using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using GongSolutions.Wpf.DragDrop;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.AllContentList;
using PointlessWaymarks.CmsWpfControls.ContentMap;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.ImageContentEditor;
using PointlessWaymarks.CmsWpfControls.ImageList;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.LinkList;
using PointlessWaymarks.CmsWpfControls.ListFilterBuilder;
using PointlessWaymarks.CmsWpfControls.MapComponentList;
using PointlessWaymarks.CmsWpfControls.NoteList;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CmsWpfControls.PostList;
using PointlessWaymarks.CmsWpfControls.Utility.Excel;
using PointlessWaymarks.CmsWpfControls.VideoContentEditor;
using PointlessWaymarks.CmsWpfControls.VideoList;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ColumnSort;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;
using ColumnSortControlContext = PointlessWaymarks.WpfCommon.ColumnSort.ColumnSortControlContext;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class ContentListContext : IDragSource, IDropTarget
{
    private ContentListContext(StatusControlContext? statusContext,
        ObservableCollection<IContentListItem> factoryContentListItems,
        ContentListSelected<IContentListItem> factoryListSelection, ListFilterBuilderContext factoryListFilterBuilder,
        IContentListLoader loader,
        WindowIconStatus? windowStatus = null)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        StatusContext.PropertyChanged += StatusContextOnPropertyChanged;

        WindowStatus = windowStatus;

        BuildCommands();

        ContentListLoader = loader;

        Items = factoryContentListItems;

        ListSelection = factoryListSelection;

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
        VideoItemActions = new VideoContentActions(StatusContext);

        NewActions = new CmsCommonCommands(StatusContext, WindowStatus);

        ListFilterBuilder = factoryListFilterBuilder;

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        ListSort = ContentListLoader.SortContext();

        ListSort.SortUpdated += (_, list) =>
            StatusContext.RunFireAndForgetNonBlockingTask(() => ListContextSortHelpers.SortList(list, Items));

        PropertyChanged += OnPropertyChanged;
    }

    public IContentListLoader ContentListLoader { get; set; }
    public List<ContextMenuItemData> ContextMenuItems { get; set; } = [];
    public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }
    public FileContentActions FileItemActions { get; set; }
    public bool FilterOnUiShown { get; set; }
    public GeoJsonContentActions GeoJsonItemActions { get; set; }
    public ImageContentActions ImageItemActions { get; set; }
    public ObservableCollection<IContentListItem> Items { get; set; }
    public LineContentActions LineItemActions { get; set; }
    public LinkContentActions LinkItemActions { get; set; }
    public ListFilterBuilderContext ListFilterBuilder { get; set; }
    public ContentListSelected<IContentListItem> ListSelection { get; set; }
    public ColumnSortControlContext ListSort { get; set; }
    public MapComponentContentActions MapComponentItemActions { get; set; }
    public CmsCommonCommands NewActions { get; set; }
    public NoteContentActions NoteItemActions { get; set; }
    public PhotoContentActions PhotoItemActions { get; set; }
    public PointContentActions PointItemActions { get; set; }
    public PostContentActions PostItemActions { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string? UserFilterText { get; set; }
    public VideoContentActions VideoItemActions { get; set; }
    public WindowIconStatus? WindowStatus { get; set; }

    public bool CanStartDrag(IDragInfo dragInfo)
    {
        return SelectedListItems().Count > 0;
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
        var defaultBracketCodeList = SelectedListItems().Select(x => x.DefaultBracketCode()).ToList();
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
            ".FLAC",
            ".MP3",
            ".WAV",
            ".JPG",
            ".JPEG",
            ".GPX",
            ".TCX",
            ".FIT",
            ".MP4",
            ".OGG",
            ".WEBM",

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

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task BracketCodeToClipboardSelected(CancellationToken cancelToken)
    {
        var currentSelected = SelectedListItems();

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

    public static async Task<List<object>> CreatedOnDayFilter(DateTime? createdOn)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (createdOn == null) return [];

        return (await Db.ContentCreatedOnDay(createdOn.Value)).ToList();
    }

    [NonBlockingCommand]
    public static async Task CreatedOnDaySearch(DateTime? filter)
    {
        await RunReport(async () => await CreatedOnDayFilter(filter), $"Created On Search - {filter}");
    }

    public static async Task<ContentListContext> CreateInstance(StatusControlContext? statusContext,
        IContentListLoader loader, List<string> searchBuilderContentTypes, WindowIconStatus? windowStatus = null)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var factoryObservable = new ObservableCollection<IContentListItem>();

        await ThreadSwitcher.ResumeBackgroundAsync();
        var factoryContext = statusContext ?? new StatusControlContext();
        var factoryListSelection = await ContentListSelected<IContentListItem>.CreateInstance(factoryContext);
        var factorySearchBuilder = await ListFilterBuilderContext.CreateInstance(searchBuilderContentTypes);

        return new ContentListContext(statusContext, factoryObservable, factoryListSelection, factorySearchBuilder,
            loader, windowStatus);
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
                dbItems = (await (await context.PointContents
                    .Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).ToPointContentDto(context)).Cast<IContentId>().ToList();
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

            if (loopItem is IMainImage mainImage && existingItem is IContentListImage itemWithListImage)
            {
                var (smallImageUrl, displayImageUrl) = GetContentItemImageUrls(mainImage);
                itemWithListImage.SmallImageUrl = smallImageUrl;
                itemWithListImage.DisplayImageUrl = displayImageUrl;
            }
        }

        if (StatusContext.BlockUi) FilterOnUiShown = true;
        else StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItemsAskIfOverMax(MaxSelectedItems = 10, ActionVerb = "delete")]
    public async Task DeleteSelected(CancellationToken cancelToken)
    {
        var currentSelected = SelectedListItems();

        foreach (var loopSelected in currentSelected)
        {
            cancelToken.ThrowIfCancellationRequested();

            await loopSelected.Delete();
        }
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItemsAskIfOverMax(MaxSelectedItems = 10, ActionVerb = "edit")]
    public async Task EditSelected(CancellationToken cancelToken)
    {
        var currentSelected = SelectedListItems();

        foreach (var loopSelected in currentSelected)
        {
            cancelToken.ThrowIfCancellationRequested();
            await loopSelected.Edit();
        }
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task ExtractNewLinksSelected(CancellationToken cancelToken)
    {
        var currentSelected = SelectedListItems();

        foreach (var loopSelected in currentSelected)
        {
            cancelToken.ThrowIfCancellationRequested();
            await loopSelected.ExtractNewLinks();
        }
    }

    public async Task<List<IContentListItem>> FilteredListItems()
    {
        var returnList = new List<IContentListItem>();

        await ThreadSwitcher.ResumeForegroundAsync();

        var itemsView = CollectionViewSource.GetDefaultView(Items);

        var filter = itemsView.Filter;

        if (filter is null) return Items.ToList();

        foreach (var loopView in itemsView)
        {
            if (!filter(loopView)) continue;

            if (loopView is IContentListItem itemList) returnList.Add(itemList);
        }

        return returnList;
    }

    private async Task FilterList()
    {
        if (!Items.Any()) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        var itemsView = CollectionViewSource.GetDefaultView(Items);

        if (string.IsNullOrWhiteSpace(UserFilterText))
        {
            itemsView.Filter = _ => true;
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

            if (searchString.StartsWith("SUMMARY:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchSummary;
            else if (searchString.StartsWith("TITLE:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchTitle;
            else if (searchString.StartsWith("TYPE:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchContentType;
            else if (searchString.StartsWith("FOLDER:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchFolder;
            else if (searchString.StartsWith("CREATED ON:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchCreatedOn;
            else if (searchString.StartsWith("CREATED BY:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchCreatedBy;
            else if (searchString.StartsWith("LAST UPDATED ON:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchLastUpdatedOn;
            else if (searchString.StartsWith("LAST UPDATED BY:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchLastUpdatedBy;
            else if (searchString.StartsWith("IN MAIN SITE FEED:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchShowInMainSiteFeed;
            else if (searchString.StartsWith("TAGS:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchTags;
            else if (searchString.StartsWith("SLUG:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchSlug;
            else if (searchString.StartsWith("UPDATE NOTES:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchUpdateNotes;
            else if (searchString.StartsWith("ORIGINAL FILE NAME:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchOriginalFileName;
            else if (searchString.StartsWith("FILE EMBED:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchFileFileEmbed;
            else if (searchString.StartsWith("CAMERA:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchCamera;
            else if (searchString.StartsWith("LENS:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchLens;
            else if (searchString.StartsWith("LICENSE:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchLicense;
            else if (searchString.StartsWith("PHOTO CREATED ON:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchPhotoCreatedOn;
            else if (searchString.StartsWith("APERTURE:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchAperture;
            else if (searchString.StartsWith("SHUTTER SPEED:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchShutterSpeed;
            else if (searchString.StartsWith("ISO:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchIso;
            else if (searchString.StartsWith("FOCAL LENGTH:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchFocalLength;
            else if (searchString.StartsWith("PHOTO SHOW POSITION:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchPhotoPosition;
            else if (searchString.StartsWith("SHOW PICTURE SIZES:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchPictureShowSizes;
            else if (searchString.StartsWith("IMAGE IN SEARCH:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchImageShowInSearch;
            else if (searchString.StartsWith("MILES:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchMiles;
            else if (searchString.StartsWith("MIN ELEVATION:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchMinElevation;
            else if (searchString.StartsWith("MAX ELEVATION:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchMaxElevation;
            else if (searchString.StartsWith("CLIMB:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchClimb;
            else if (searchString.StartsWith("DESCENT:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchDescent;
            else if (searchString.StartsWith("IN ACTIVITY LOG:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchIncludeInActivityLog;
            else if (searchString.StartsWith("ACTIVITY TYPE:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchActivityType;
            else if (searchString.StartsWith("MAP CONTENT REFERENCES:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchLineShowContentReferencesOnMap;
            else if (searchString.StartsWith("MAP LABEL:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchMapLabel;
            else if (searchString.StartsWith("MAP ICON:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchMapIcon;
            else if (searchString.StartsWith("MAP MARKER COLOR:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchMapMarkerColor;
            else if (searchString.StartsWith("PUBLIC DOWNLOAD:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchPublicDownloadLink;
            else if (searchString.StartsWith("ELEVATION:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchElevation;
            else if (searchString.StartsWith("BOUNDS:", StringComparison.InvariantCultureIgnoreCase))
                searchFilterFunction = ContentListSearch.SearchBounds;
            else searchFilterFunction = ContentListSearch.SearchGeneral;

            searchFilterStack.Add((searchFilterFunction, searchString, searchResultModifier));
        }

        if (!searchFilterStack.Any())
        {
            itemsView.Filter = _ => true;
            return;
        }

        itemsView.Filter = o =>
        {
            if (o is not IContentListItem toFilter) return false;

            var filterResults = searchFilterStack.Select(x => x.filter(toFilter, x.searchString, x.searchModifier))
                .ToList();

            return !filterResults.Any() || filterResults.All(x => x.ResultModifier(x.SearchFunctionReturn.Include));
        };
    }

    public static async Task<List<object>> FolderFilter(string? folderName)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        return (await Db.ContentInFolder(folderName ?? string.Empty)).ToList();
    }

    [NonBlockingCommand]
    public static async Task FolderSearch(string filter)
    {
        await RunReport(async () => await FolderFilter(filter), $"Folder Search - {filter}");
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task GenerateHtmlSelected(CancellationToken cancelToken)
    {
        var currentSelected = SelectedListItems();

        foreach (var loopSelected in currentSelected)
        {
            cancelToken.ThrowIfCancellationRequested();
            await loopSelected.GenerateHtml();
        }
    }

    public static (string? smallImageUrl, string? displayImageUrl) GetContentItemImageUrls(IMainImage? content)
    {
        if (content?.MainPicture == null) return (null, null);

        string? smallImageUrl;
        string? displayImageUrl;

        try
        {
            var pictureAsset = PictureAssetProcessing.ProcessPictureDirectory(content.MainPicture.Value);
            smallImageUrl = pictureAsset?.SmallPicture?.File?.FullName;
            displayImageUrl = pictureAsset?.DisplayPicture?.File?.FullName;
        }
        catch
        {
            smallImageUrl = null;
            displayImageUrl = null;
        }

        return (smallImageUrl, displayImageUrl);
    }

    [BlockingCommand]
    public async Task ImportFromExcelFile()
    {
        await ExcelHelpers.ImportFromExcelFile(StatusContext);
    }

    [BlockingCommand]
    public async Task ImportFromOpenExcelInstance()
    {
        await ExcelHelpers.ImportFromOpenExcelInstance(StatusContext);
    }

    public ICollectionView ItemsView()
    {
        return CollectionViewSource.GetDefaultView(Items);
    }

    public async Task<IContentListItem?> ListItemFromDbItem(object? dbItem)
    {
        //!!Content List
        return dbItem switch
        {
            FileContent f =>
                await FileContentActions.ListItemFromDbItem(f, FileItemActions, ContentListLoader.ShowType),
            GeoJsonContent g => await GeoJsonContentActions.ListItemFromDbItem(g, GeoJsonItemActions,
                ContentListLoader.ShowType),
            ImageContent g => await ImageContentActions.ListItemFromDbItem(g, ImageItemActions,
                ContentListLoader.ShowType),
            LineContent l =>
                await LineContentActions.ListItemFromDbItem(l, LineItemActions, ContentListLoader.ShowType),
            LinkContent k =>
                await LinkContentActions.ListItemFromDbItem(k, LinkItemActions, ContentListLoader.ShowType),
            MapComponent m => await MapComponentContentActions.ListItemFromDbItem(m, MapComponentItemActions,
                ContentListLoader.ShowType),
            NoteContent n =>
                await NoteContentActions.ListItemFromDbItem(n, NoteItemActions, ContentListLoader.ShowType),
            PhotoContent ph => await PhotoContentActions.ListItemFromDbItem(ph, PhotoItemActions,
                ContentListLoader.ShowType),
            PointContent pt => await PointContentActions.ListItemFromDbItem(pt, PointItemActions,
                ContentListLoader.ShowType),
            PointContentDto ptd => await PointContentActions.ListItemFromDbItem(ptd, PointItemActions,
                ContentListLoader.ShowType),
            PostContent po => await PostContentActions.ListItemFromDbItem(po, PostItemActions,
                ContentListLoader.ShowType),
            VideoContent v => await VideoContentActions.ListItemFromDbItem(v, VideoItemActions,
                ContentListLoader.ShowType),
            _ => null
        };
    }

    [BlockingCommand]
    public async Task LoadAll()
    {
        ContentListLoader.PartialLoadQuantity = null;
        await LoadData();
    }

    /// <summary>
    ///     Loads data - by default it will create a new ObservableCollection which is very performant and works
    ///     in most cases, but if you have event handlers attached to the collection you may want to preserve the
    ///     collection with preserveCollection = true
    /// </summary>
    /// <param name="preserveCollection"></param>
    /// <returns></returns>
    public async Task LoadData(bool preserveCollection = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

        StatusContext.Progress("Starting Item Load");

        var dbItems = await ContentListLoader.LoadItems(StatusContext.ProgressTracker());

        StatusContext.Progress($"All Items Loaded from Db: {ContentListLoader.AllItemsLoaded}");

        var contentListItems = new ConcurrentBag<IContentListItem>();

        StatusContext.Progress("Creating List Items");

        var loopCounter = 0;

        await Parallel.ForEachAsync(dbItems, async (loopDbItem, _) =>
        {
            Interlocked.Increment(ref loopCounter);

            if (loopCounter % 250 == 0)
                StatusContext.Progress($"Created List Item {loopCounter} of {dbItems.Count}");

            var listItem = await ListItemFromDbItem(loopDbItem);

            if (listItem == null) return;

            contentListItems.Add(listItem);
        });

        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Loading Display List of Items");

        if (preserveCollection)
        {
            Items.Clear();
            foreach (var toAdd in contentListItems) Items.Add(toAdd);
        }
        else
        {
            Items = new ObservableCollection<IContentListItem>(contentListItems);
        }

        await ListContextSortHelpers.SortList(ListSort.SortDescriptions(), Items);
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

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task PictureGalleryBracketCodeToClipboardSelected(CancellationToken cancelToken)
    {
        var currentSelected = SelectedListItems();

        var bracketCodes = new List<string>();

        foreach (var loopSelected in currentSelected)
        {
            var toAdd = loopSelected switch //!!Content List
            {
                FileListListItem f => BracketCodeFileImageLink.Create(f.DbEntry),
                ImageListListItem i => BracketCodeImages.Create(i.DbEntry),
                GeoJsonListListItem g => BracketCodeGeoJsonImageLink.Create(g.DbEntry),
                LineListListItem l => BracketCodeLineImageLink.Create(l.DbEntry),
                PhotoListListItem p => BracketCodePhotos.Create(p.DbEntry),
                PointListListItem pt => BracketCodePointImageLink.Create(pt.DbEntry.ToDbObject()),
                PostListListItem po => BracketCodePostImageLink.Create(po.DbEntry),
                VideoListListItem v => BracketCodeVideoImageLink.Create(v.DbEntry),
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(toAdd)) bracketCodes.Add(toAdd);
        }

        var individualCodes = string.Join(Environment.NewLine, bracketCodes.Where(x => !string.IsNullOrWhiteSpace(x)));

        if (string.IsNullOrWhiteSpace(individualCodes))
        {
            StatusContext.ToastSuccess("No Bracket Codes Found?");
            return;
        }

        var finalString = GalleryBracketCodePictures.Create(individualCodes);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess("Bracket Codes copied to Clipboard");
    }

    private async Task PossibleMainImageUpdateDataNotificationReceived(InterProcessDataNotification? translatedMessage)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (translatedMessage?.ContentIds == null) return;

        var smallImageListItems = Items.Where(x => x is IContentListImage).Cast<IContentListImage>().ToList();

        foreach (var loopListItem in smallImageListItems)
            if (((dynamic)loopListItem).DbEntry is IMainImage { MainPicture: not null } dbMainImageEntry &&
                translatedMessage.ContentIds.Contains(dbMainImageEntry.MainPicture.Value))
            {
                var (smallImageUrl, displayImageUrl) = GetContentItemImageUrls(dbMainImageEntry);
                loopListItem.SmallImageUrl = smallImageUrl;
                loopListItem.DisplayImageUrl = displayImageUrl;
            }
    }

    public static async Task RunReport(Func<Task<List<object>>> toRun, string title)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var reportLoader = new ContentListLoaderReport(toRun, ContentListLoaderBase.SortContextDefault());

        var newWindow =
            await AllContentListWindow.CreateInstance(
                await AllContentListWithActionsContext.CreateInstance(null, reportLoader));
        newWindow.WindowTitle = title;

        await newWindow.PositionWindowAndShowOnUiThread();
    }


    [NonBlockingCommand]
    private async Task SearchBuildHelperWindow()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var newWindow = await ListFilterBuilderWindow.CreateInstance(ListFilterBuilder);
        newWindow.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive) ??
                          Application.Current.Windows.OfType<Window>().FirstOrDefault();
        newWindow.ShowDialog();
        if (newWindow.WindowExitType == SearchBuilderWindowExitType.RunSearch)
            UserFilterText = newWindow.SearchString;
        await ThreadSwitcher.ResumeForegroundAsync();
    }

    public List<IContentListItem> SelectedListItems()
    {
        return ListSelection.SelectedItems;
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task SelectedToExcel()
    {
        await ExcelHelpers.SelectedToExcel(SelectedListItems().Cast<dynamic>().ToList(), StatusContext);
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task SpatialItemsToContentMapWindowSelected(CancellationToken cancelToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var currentSelected = SelectedListItems();

        var spatialContent =
            await Db.ContentIdsAreSpatialContentInDatabase(
                currentSelected.Select(x => x.ContentId()).Where(x => x != null).Select(x => x!.Value).ToList(), true);

        if (!spatialContent.Any())
        {
            StatusContext.ToastError("No Spatial Content Selected?");
            return;
        }

        if (spatialContent.Count != currentSelected.Count)
            StatusContext.ToastWarning(
                $"{currentSelected.Count - spatialContent.Count} Selected Items not sent to the map - no spatial data...");

        cancelToken.ThrowIfCancellationRequested();

        var mapWindow =
            await ContentMapWindow.CreateInstance(new ContentMapListLoader("Mapped Content", spatialContent));

        await mapWindow.PositionWindowAndShowOnUiThread();
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

        var fileContentExtensions = new List<string> { ".PDF", ".MPG", ".MPEG", ".FLAC", ".MP3", ".WAV" };
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
                await CmsCommonCommands.NewLineContentFromFilesBase(fileInfo.AsList(), false, false,
                    CancellationToken.None,
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

                    var exifSubIfdDirectory = ImageMetadataReader.ReadMetadata(loopFile).OfType<ExifSubIfdDirectory>()
                        .FirstOrDefault();

                    make = exifDirectory?.GetDescription(ExifDirectoryBase.TagMake) ??
                           exifSubIfdDirectory?.GetDescription(ExifDirectoryBase.TagMake) ?? string.Empty;
                    model = exifDirectory?.GetDescription(ExifDirectoryBase.TagModel) ??
                            exifSubIfdDirectory?.GetDescription(ExifDirectoryBase.TagModel) ?? string.Empty;
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

    public static async Task<List<object>> UpdatedOnDayFilter(DateTime? createdOn)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        return createdOn == null
            ? (await Db.ContentNeverUpdated()).ToList()
            : (await Db.ContentUpdatedOnDay(createdOn.Value)).ToList();
    }

    [NonBlockingCommand]
    public static async Task UpdatedOnDaySearch(DateTime? filter)
    {
        await RunReport(async () => await UpdatedOnDayFilter(filter), $"Updated On Search - {filter}");
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItemsAskIfOverMax(MaxSelectedItems = 10, ActionVerb = "view history")]
    public async Task ViewHistorySelected(CancellationToken cancelToken)
    {
        var currentSelected = SelectedListItems();

        foreach (var loopSelected in currentSelected)
        {
            cancelToken.ThrowIfCancellationRequested();
            await loopSelected.ViewHistory();
        }
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItemsAskIfOverMax(MaxSelectedItems = 10, ActionVerb = "view")]
    public async Task ViewOnSite(CancellationToken cancelToken)
    {
        var currentSelected = SelectedListItems();

        foreach (var loopSelected in currentSelected)
        {
            cancelToken.ThrowIfCancellationRequested();
            await loopSelected.ViewOnSite();
        }
    }
}