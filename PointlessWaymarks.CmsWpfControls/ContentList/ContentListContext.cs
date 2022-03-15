using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Shell;
using System.Windows.Threading;
using GongSolutions.Wpf.DragDrop;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.AllContentList;
using PointlessWaymarks.CmsWpfControls.ColumnSort;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.ImageList;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.LinkList;
using PointlessWaymarks.CmsWpfControls.MapComponentList;
using PointlessWaymarks.CmsWpfControls.NoteList;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CmsWpfControls.PostList;
using PointlessWaymarks.CmsWpfControls.S3Uploads;
using PointlessWaymarks.CmsWpfControls.SitePreview;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

[ObservableObject]
public partial class ContentListContext : IDragSource, IDropTarget
{
    [ObservableProperty] private RelayCommand _bracketCodeToClipboardSelectedCommand;
    [ObservableProperty] private IContentListLoader _contentListLoader;
    [ObservableProperty] private List<ContextMenuItemData> _contextMenuItems;
    [ObservableProperty] private RelayCommand<DateTime?> _createdOnDaySearchCommand;
    [ObservableProperty] private DataNotificationsWorkQueue _dataNotificationsProcessor;
    [ObservableProperty] private RelayCommand _deleteSelectedCommand;
    [ObservableProperty] private RelayCommand _editSelectedCommand;
    [ObservableProperty] private RelayCommand _extractNewLinksSelectedCommand;
    [ObservableProperty] private FileContentActions _fileItemActions;
    [ObservableProperty] private RelayCommand<string> _folderSearchCommand;
    [ObservableProperty] private RelayCommand _generateChangedHtmlAndShowSitePreviewCommand;
    [ObservableProperty] private RelayCommand _generateChangedHtmlAndStartUploadCommand;
    [ObservableProperty] private RelayCommand _generateChangedHtmlCommand;
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
    [ObservableProperty] private NewContent _newActions;

    [ObservableProperty] private RelayCommand _newAllContentListWindowCommand;
    [ObservableProperty] private RelayCommand _newFileListWindowCommand;
    [ObservableProperty] private RelayCommand _newGeoJsonListWindowCommand;
    [ObservableProperty] private RelayCommand _newImageListWindowCommand;
    [ObservableProperty] private RelayCommand _newLineListWindowCommand;
    [ObservableProperty] private RelayCommand _newLinkListWindowCommand;
    [ObservableProperty] private RelayCommand _newMapComponentListWindowCommand;
    [ObservableProperty] private RelayCommand _newNoteListWindowCommand;
    [ObservableProperty] private RelayCommand _newPhotoListWindowCommand;
    [ObservableProperty] private RelayCommand _newPointListWindowCommand;
    [ObservableProperty] private RelayCommand _newPostListWindowCommand;
    [ObservableProperty] private NoteContentActions _noteItemActions;
    [ObservableProperty] private PhotoContentActions _photoItemActions;
    [ObservableProperty] private PointContentActions _pointItemActions;
    [ObservableProperty] private PostContentActions _postItemActions;
    [ObservableProperty] private RelayCommand _selectedToExcelCommand;
    [ObservableProperty] private RelayCommand _showSitePreviewWindowCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _userFilterText;
    [ObservableProperty] private RelayCommand _viewHistorySelectedCommand;
    [ObservableProperty] private RelayCommand _viewOnSiteCommand;
    [ObservableProperty] private WindowIconStatus _windowStatus;


    public ContentListContext(StatusControlContext statusContext, IContentListLoader loader,
        WindowIconStatus windowStatus = null)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;

        PropertyChanged += OnPropertyChanged;

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

        NewActions = new NewContent(StatusContext, WindowStatus);

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        LoadAllCommand = StatusContext.RunBlockingTaskCommand(async () =>
        {
            ContentListLoader.PartialLoadQuantity = null;
            await LoadData();
        });

        DeleteSelectedCommand = StatusContext.RunBlockingTaskWithCancellationCommand(DeleteSelected, "Cancel Delete");
        BracketCodeToClipboardSelectedCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(BracketCodeToClipboardSelected, "Cancel Delete");
        EditSelectedCommand = StatusContext.RunBlockingTaskWithCancellationCommand(EditSelected, "Cancel Edit");
        ExtractNewLinksSelectedCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(ExtractNewLinksSelected, "Cancel Link Extraction");
        GenerateHtmlSelectedCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(GenerateHtmlSelected, "Cancel Generate Html");
        ViewOnSiteCommand = StatusContext.RunBlockingTaskWithCancellationCommand(ViewOnSiteSelected, "Cancel Open Url");
        ViewHistorySelectedCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(ViewHistorySelected, "Cancel View History");

        ImportFromExcelFileCommand =
            StatusContext.RunBlockingTaskCommand(async () => await ExcelHelpers.ImportFromExcelFile(StatusContext));
        ImportFromOpenExcelInstanceCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await ExcelHelpers.ImportFromOpenExcelInstance(StatusContext));
        SelectedToExcelCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
            await ExcelHelpers.SelectedToExcel(ListSelection.SelectedItems?.Cast<dynamic>().ToList(), StatusContext));

        GenerateChangedHtmlAndStartUploadCommand =
            StatusContext.RunBlockingTaskCommand(GenerateChangedHtmlAndStartUpload);
        GenerateChangedHtmlCommand = StatusContext.RunBlockingTaskCommand(GenerateChangedHtml);
        ShowSitePreviewWindowCommand = StatusContext.RunNonBlockingTaskCommand(ShowSitePreviewWindow);
        GenerateChangedHtmlAndShowSitePreviewCommand =
            StatusContext.RunBlockingTaskCommand(GenerateChangedHtmlAndShowSitePreview);

        FolderSearchCommand = StatusContext.RunNonBlockingTaskCommand<string>(async x =>
            await RunReport(async () => await FolderSearch(x), $"Folder Search - {x}"));
        CreatedOnDaySearchCommand = StatusContext.RunNonBlockingTaskCommand<DateTime?>(async x =>
            await RunReport(async () => await CreatedOnDaySearch(x), $"Created On Search - {x}"));
        LastUpdatedOnDaySearchCommand = StatusContext.RunNonBlockingTaskCommand<DateTime?>(async x =>
            await RunReport(async () => await UpdatedOnDaySearch(x), $"Last Updated On Search - {x}"));

        NewAllContentListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            var newWindow = new AllContentListWindow
                { ListContext = new AllContentListWithActionsContext(null, WindowStatus) };
            newWindow.PositionWindowAndShow();
        });
        NewFileListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            var newWindow = new FileListWindow { ListContext = new FileListWithActionsContext(null, WindowStatus) };
            newWindow.PositionWindowAndShow();
        });
        NewGeoJsonListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            var newWindow = new GeoJsonListWindow
                { ListContext = new GeoJsonListWithActionsContext(null, WindowStatus) };
            newWindow.PositionWindowAndShow();
        });
        NewImageListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            var newWindow = new ImageListWindow { ListContext = new ImageListWithActionsContext(null, WindowStatus) };
            newWindow.PositionWindowAndShow();
        });
        NewLineListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            var newWindow = new LineListWindow { ListContext = new LineListWithActionsContext(null, WindowStatus) };
            newWindow.PositionWindowAndShow();
        });
        NewLinkListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            var newWindow = new LinkListWindow { ListContext = new LinkListWithActionsContext(null, WindowStatus) };
            newWindow.PositionWindowAndShow();
        });
        NewMapComponentListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            var newWindow = new MapComponentListWindow
                { ListContext = new MapComponentListWithActionsContext(null, WindowStatus) };
            newWindow.PositionWindowAndShow();
        });
        NewNoteListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            var newWindow = new NoteListWindow { ListContext = new NoteListWithActionsContext(null, WindowStatus) };
            newWindow.PositionWindowAndShow();
        });
        NewPhotoListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            var newWindow = new PhotoListWindow { ListContext = new PhotoListWithActionsContext(null, WindowStatus) };
            newWindow.PositionWindowAndShow();
        });
        NewPointListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            var newWindow = new PointListWindow { ListContext = new PointListWithActionsContext(null, WindowStatus) };
            newWindow.PositionWindowAndShow();
        });
        NewPostListWindowCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            var newWindow = new PostListWindow { ListContext = new PostListWithActionsContext(null, WindowStatus) };
            newWindow.PositionWindowAndShow();
        });
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
            ".FIT"
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

            var filterLineResults = new List<bool>();

            using var sr = new StringReader(UserFilterText);

            while (sr.ReadLine() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var searchString = line.Trim();
                Func<bool, bool> searchResultModifier = x => x;

                if (line.ToUpper().StartsWith("-"))
                {
                    searchResultModifier = x => !x;
                    searchString = line[1..];
                    searchString = searchString.Trim();
                }

                if (string.IsNullOrWhiteSpace(searchString)) continue;

                if (searchString.ToUpper().StartsWith("TITLE:"))
                {
                    searchString = searchString[6..];
                    if (string.IsNullOrWhiteSpace(searchString)) continue;
                    searchString = searchString.Trim();

                    filterLineResults.Add(searchResultModifier(
                        (toFilter.Content().Title ?? string.Empty).Contains(searchString,
                            StringComparison.OrdinalIgnoreCase)));
                    continue;
                }

                if (searchString.ToUpper().StartsWith("SUMMARY:"))
                {
                    searchString = searchString[8..];
                    if (string.IsNullOrWhiteSpace(searchString)) continue;
                    searchString = searchString.Trim();

                    filterLineResults.Add(searchResultModifier(
                        (toFilter.Content().Summary ?? string.Empty).Contains(searchString,
                            StringComparison.OrdinalIgnoreCase)));
                    continue;
                }

                if (searchString.ToUpper().StartsWith("FOLDER:"))
                {
                    searchString = searchString[7..];
                    if (string.IsNullOrWhiteSpace(searchString)) continue;
                    searchString = searchString.Trim();

                    filterLineResults.Add(searchResultModifier(
                        (toFilter.Content().Folder ?? string.Empty).Contains(searchString,
                            StringComparison.OrdinalIgnoreCase)));
                    continue;
                }

                if (searchString.ToUpper().StartsWith("TAGS:"))
                {
                    searchString = searchString[5..];
                    if (string.IsNullOrWhiteSpace(searchString)) continue;
                    searchString = searchString.Trim();

                    filterLineResults.Add(searchResultModifier(
                        (toFilter.Content().Tags ?? string.Empty).Contains(searchString,
                            StringComparison.OrdinalIgnoreCase)));
                    continue;
                }

                if (searchString.ToUpper().StartsWith("CAMERA:"))
                {
                    searchString = searchString[7..];
                    if (string.IsNullOrWhiteSpace(searchString)) continue;

                    if (o is not PhotoListListItem photoItem) return false;
                    if (string.IsNullOrWhiteSpace(photoItem.DbEntry.CameraMake) && string.IsNullOrWhiteSpace(photoItem.DbEntry.CameraModel)) return searchResultModifier(false);

                    var cameraMakeModel =
                        $"{photoItem.DbEntry.CameraMake.TrimNullToEmpty()} {photoItem.DbEntry.CameraModel.TrimNullToEmpty()}";
                    searchString = searchString.Trim();

                    filterLineResults.Add(searchResultModifier(
                        (cameraMakeModel).Contains(searchString,
                            StringComparison.OrdinalIgnoreCase)));
                    continue;
                }

                if (searchString.ToUpper().StartsWith("ISO:"))
                {
                    //As soon as we designate an ISO search limit the search to photos with Iso
                    if (o is not PhotoListListItem photoItem) return false;
                    if (photoItem.DbEntry.Iso == null) return searchResultModifier(false);

                    searchString = searchString[4..];
                    if (string.IsNullOrWhiteSpace(searchString)) continue;
                    var spaceSplitTokens = searchString.Split(" ").Select(x => x.TrimNullToEmpty())
                        .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x == "=" ? "==" : x).ToList();

                    var tokens = new List<string>();

                    var singleCharacterOperators = new List<char> { '>', '=', '<' };
                    var twoCharacterOperators = new List<string> { ">=", "==", "<=" };
                    var operators = new List<string> { "==", ">", "<", ">=", "<=" };


                    foreach (var loopTokens in spaceSplitTokens)
                    {
                        if (twoCharacterOperators.Contains(loopTokens[..1]) && loopTokens.Length > 2)
                        {
                            tokens.Add(loopTokens[..1]);
                            tokens.Add(loopTokens[2..]);
                            continue;
                        }

                        if (singleCharacterOperators.Contains(loopTokens[0]) && loopTokens.Length > 1)
                        {
                            tokens.Add(loopTokens[0].ToString());
                            tokens.Add(loopTokens[1..]);
                            continue;
                        }

                        tokens.Add(loopTokens);
                    }

                    if (!tokens.Any()) continue;
                    if (tokens.Count == 1)
                    {
                        if (!int.TryParse(tokens.First(), out var parsedIso)) continue;
                        filterLineResults.Add(searchResultModifier(photoItem.DbEntry.Iso == parsedIso));
                    }

                    var isoSearchResults = new List<bool>();

                    for (var i = 0; i < tokens.Count; i++)
                    {
                        var scanValue = tokens[i];
                        if (!operators.Contains(scanValue))
                        {
                            if (!int.TryParse(scanValue, out var parsedIso)) continue;
                            isoSearchResults.Add(photoItem.DbEntry.Iso == parsedIso);
                            continue;
                        }

                        i++;
                        if (i >= tokens.Count) continue;

                        var lookaheadValue = tokens[i];

                        if (!int.TryParse(lookaheadValue, out var parsedIsoForExpression)) continue;
                        switch (scanValue)
                        {
                            case "==":
                                isoSearchResults.Add(photoItem.DbEntry.Iso == parsedIsoForExpression);
                                break;
                            case ">":
                                isoSearchResults.Add(photoItem.DbEntry.Iso > parsedIsoForExpression);
                                break;
                            case ">=":
                                isoSearchResults.Add(photoItem.DbEntry.Iso >= parsedIsoForExpression);
                                break;
                            case "<":
                                isoSearchResults.Add(photoItem.DbEntry.Iso < parsedIsoForExpression);
                                break;
                            case "<=":
                                isoSearchResults.Add(photoItem.DbEntry.Iso <= parsedIsoForExpression);
                                break;
                        }
                    }

                    filterLineResults.Add(!isoSearchResults.Any()
                        ? searchResultModifier(false)
                        : searchResultModifier(isoSearchResults.All(x => x)));

                    continue;
                }

                bool AllFieldsSearch(string stringToSearch)
                {
                    if ((toFilter.Content().Title ?? string.Empty).Contains(stringToSearch,
                            StringComparison.OrdinalIgnoreCase))
                        return true;
                    if ((toFilter.Content().Tags ?? string.Empty).Contains(stringToSearch,
                            StringComparison.OrdinalIgnoreCase))
                        return true;
                    if ((toFilter.Content().Summary ?? string.Empty).Contains(stringToSearch,
                            StringComparison.OrdinalIgnoreCase)) return true;
                    if ((toFilter.Content().Folder ?? string.Empty).Contains(stringToSearch,
                            StringComparison.OrdinalIgnoreCase)) return true;
                    if ((toFilter.Content().CreatedBy ?? string.Empty).Contains(stringToSearch,
                            StringComparison.OrdinalIgnoreCase)) return true;
                    if ((toFilter.Content().LastUpdatedBy ?? string.Empty).Contains(stringToSearch,
                            StringComparison.OrdinalIgnoreCase)) return true;
                    if (toFilter.ContentId() != null && toFilter.ContentId().ToString()
                            .Contains(stringToSearch, StringComparison.OrdinalIgnoreCase)) return true;
                    return false;
                }

                filterLineResults.Add(searchResultModifier(AllFieldsSearch(searchString)));
            }

            return !filterLineResults.Any() || filterLineResults.All(x => x);
        };
    }

    public static async Task<List<object>> FolderSearch(string folderName)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        return (await Db.ContentInFolder(folderName ?? string.Empty)).ToList();
    }

    private async Task GenerateChangedHtml()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        try
        {
            WindowStatus?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.Indeterminate));

            await HtmlGenerationGroups.GenerateChangedToHtml(StatusContext.ProgressTracker());
        }
        finally
        {
            WindowStatus?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.None));
        }
    }

    private async Task GenerateChangedHtmlAndShowSitePreview()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        try
        {
            WindowStatus?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.Indeterminate));

            await HtmlGenerationGroups.GenerateChangedToHtml(StatusContext.ProgressTracker());
        }
        finally
        {
            WindowStatus?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.None));
        }

        await ThreadSwitcher.ResumeForegroundAsync();


        var sitePreviewWindow = new SiteOnDiskPreviewWindow();
        sitePreviewWindow.PositionWindowAndShow();
    }

    private async Task GenerateChangedHtmlAndStartUpload()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        try
        {
            WindowStatus?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.Indeterminate));

            await S3UploadHelpers.GenerateChangedHtmlAndStartUpload(StatusContext, WindowStatus);
        }
        finally
        {
            WindowStatus?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.None));
        }
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
            smallImageUrl = PictureAssetProcessing.ProcessPictureDirectory(content.MainPicture.Value)?.SmallPicture
                ?.File?.FullName;
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
            ImageContent g => ImageContentActions.ListItemFromDbItem(g, ImageItemActions, ContentListLoader.ShowType),
            LineContent l => LineContentActions.ListItemFromDbItem(l, LineItemActions, ContentListLoader.ShowType),
            LinkContent k => LinkContentActions.ListItemFromDbItem(k, LinkItemActions, ContentListLoader.ShowType),
            MapComponent m => MapComponentContentActions.ListItemFromDbItem(m, MapComponentItemActions,
                ContentListLoader.ShowType),
            NoteContent n => NoteContentActions.ListItemFromDbItem(n, NoteItemActions, ContentListLoader.ShowType),
            PhotoContent ph => PhotoContentActions.ListItemFromDbItem(ph, PhotoItemActions, ContentListLoader.ShowType),
            PointContent pt => PointContentActions.ListItemFromDbItem(pt, PointItemActions, ContentListLoader.ShowType),
            PostContent po => PostContentActions.ListItemFromDbItem(po, PostItemActions, ContentListLoader.ShowType),
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

        var contentListItems = new ConcurrentBag<IContentListItem>();

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

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(UserFilterText))
            StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }

    private async Task PossibleMainImageUpdateDataNotificationReceived(InterProcessDataNotification translatedMessage)
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

        var context = new AllContentListWithActionsContext(null, reportLoader);

        await ThreadSwitcher.ResumeForegroundAsync();

        var newWindow = new AllContentListWindow { ListContext = context, WindowTitle = title };

        newWindow.PositionWindowAndShow();

        await context.LoadData();
    }

    private async Task ShowSitePreviewWindow()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var sitePreviewWindow = new SiteOnDiskPreviewWindow();

        sitePreviewWindow.PositionWindowAndShow();
    }

    private async Task TryOpenEditorsForDroppedFiles(List<string> files, StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var fileContentExtensions = new List<string> { ".PDF", ".MPG", ".MPEG", ".WAV" };
        var pictureContentExtensions = new List<string> { ".JPG", ".JPEG" };
        var lineContentExtensions = new List<string> { ".GPX", ".TCX", ".FIT" };

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
                await ThreadSwitcher.ResumeForegroundAsync();

                var newEditor = new FileContentEditorWindow(new FileInfo(loopFile));
                newEditor.PositionWindowAndShow();

                await ThreadSwitcher.ResumeBackgroundAsync();

                statusContext.ToastSuccess($"{Path.GetFileName(loopFile)} sent to File Editor");

                continue;
            }

            if (lineContentExtensions.Contains(Path.GetExtension(loopFile).ToUpperInvariant()))
            {
                await NewContent.NewLineContentFromFiles(fileInfo.AsList(), CancellationToken.None, StatusContext,
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
                    await ThreadSwitcher.ResumeForegroundAsync();

                    var photoEditorWindow = new PhotoContentEditorWindow(new FileInfo(loopFile));
                    photoEditorWindow.PositionWindowAndShow();

                    await ThreadSwitcher.ResumeBackgroundAsync();


                    statusContext.ToastSuccess($"{Path.GetFileName(loopFile)} sent to Photo Editor");
                }
                else
                {
                    await ThreadSwitcher.ResumeForegroundAsync();

                    var imageEditorWindow = new PhotoContentEditorWindow(new FileInfo(loopFile));
                    imageEditorWindow.PositionWindowAndShow();

                    await ThreadSwitcher.ResumeBackgroundAsync();

                    statusContext.ToastSuccess($"{Path.GetFileName(loopFile)} sent to Image Editor");
                }
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

    public async Task ViewOnSiteSelected(CancellationToken cancelToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (ListSelection?.SelectedItems == null || ListSelection.SelectedItems.Count < 1)
        {
            StatusContext.ToastWarning("Nothing Selected to Edit?");
            return;
        }

        var currentSelected = ListSelection.SelectedItems;

        foreach (var loopSelected in currentSelected) await loopSelected.ViewOnSite();
    }
}