using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using GongSolutions.Wpf.DragDrop;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.ImageList;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CmsWpfControls.PostList;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.VideoList;
using PointlessWaymarks.CmsWpfControls.WpfCmsHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ColumnSort;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtml;
using Serilog;
using TinyIpc.Messaging;
using ColumnSortControlContext = PointlessWaymarks.WpfCommon.ColumnSort.ColumnSortControlContext;
using ColumnSortControlSortItem = PointlessWaymarks.WpfCommon.ColumnSort.ColumnSortControlSortItem;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class MapComponentEditorContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation, IDropTarget, IWebViewMessenger
{
    public EventHandler? RequestContentEditorWindowClose;
    
    private MapComponentEditorContext(StatusControlContext statusContext,
        ContentListSelected<IMapElementListItem> factoryListSelection,
        ObservableCollection<IMapElementListItem> factoryMapList, MapComponent dbEntry, string serializedMapIcons)
    {
        StatusContext = statusContext;
        
        BuildCommands();
        
        FromWebView = new WorkQueue<FromWebViewMessage>
        {
            Processor = ProcessFromWebView
        };
        
        ToWebView = new WorkQueue<ToWebViewRequest>(true);
        
        MapPreviewNavigationManager = MapCmsJson.LocalActionNavigation(StatusContext);
        
        this.SetupCmsLeafletMapHtmlAndJs("Map", UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault, false, serializedMapIcons,
            UserSettingsSingleton.CurrentSettings().CalTopoApiKey, UserSettingsSingleton.CurrentSettings().BingApiKey);
        
        HelpContext = new HelpDisplayContext([CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock]);
        
        ListSort = SortContextMapElementsDefault();
        
        ListSort.SortUpdated += (_, list) =>
            StatusContext.RunFireAndForgetNonBlockingTask(() => ListContextSortHelpers.SortList(list, MapElements));
        
        ListSelection = factoryListSelection;
        
        DbEntry = dbEntry;
        
        MapElements = factoryMapList;
        
        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };
        
        MapElements.CollectionChanged += MapElementsOnCollectionChanged;
        
        PropertyChanged += OnPropertyChanged;
    }
    
    public Envelope? ContentBounds { get; set; }
    public ContentIdViewerControlContext? ContentId { get; set; }
    public CreatedAndUpdatedByAndOnDisplayContext? CreatedUpdatedDisplay { get; set; }
    
    public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }
    public List<MapElement> DbElements { get; set; } = [];
    public MapComponent DbEntry { get; set; }
    public HelpDisplayContext HelpContext { get; set; }
    public ContentListSelected<IMapElementListItem> ListSelection { get; set; }
    public ColumnSortControlContext ListSort { get; set; }
    public SpatialBounds? MapBounds { get; set; }
    public ObservableCollection<IMapElementListItem> MapElements { get; set; }
    public Action<Uri, string> MapPreviewNavigationManager { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public StringDataEntryContext? SummaryEntry { get; set; }
    public bool SuspendMapRefresh { get; set; }
    public StringDataEntryContext? TitleEntry { get; set; }
    public UpdateNotesEditorContext? UpdateNotes { get; set; }
    public string UserFilterText { get; set; } = string.Empty;
    public string UserGeoContentInput { get; set; } = string.Empty;
    
    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }
    
    public void DragOver(IDropInfo dropInfo)
    {
        var data = dropInfo.Data;
        if (data is string) dropInfo.Effects = DragDropEffects.Copy;
        if (data is not DataObject dataObject) return;
        if (dataObject.ContainsText()) dropInfo.Effects = DragDropEffects.Copy;
    }
    
    public async void Drop(IDropInfo dropInfo)
    {
        var textToProcess = string.Empty;
        
        var data = dropInfo.Data;
        textToProcess = data switch
        {
            string stringData => stringData,
            DataObject dataObject when dataObject.ContainsText() => dataObject.GetText(),
            _ => textToProcess
        };
        
        var contentIds = BracketCodeCommon.BracketCodeContentIds(textToProcess);
        
        SuspendMapRefresh = true;
        foreach (var loopGuids in contentIds) await TryAddSpatialType(loopGuids);
        
        await RefreshMapPreview();
        
        SuspendMapRefresh = false;
    }
    
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }
    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }
    
    private async Task AddFile(FileContent possibleFile, MapElement? loopContent = null,
        bool guiNotificationAndMapRefreshWhenAdded = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (MapElements.Any(x => x.ContentId() == possibleFile.ContentId))
        {
            await StatusContext.ToastWarning($"File {possibleFile.Title} is already on the map");
            return;
        }
        
        if (!await possibleFile.HasValidLocation())
        {
            await StatusContext.ToastWarning($"File {possibleFile.Title} doesn't have a valid location");
            return;
        }
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var newFileContent = await MapElementListFileItem.CreateInstance(new FileContentActions(StatusContext),
            possibleFile, MapElementSettings.CreateInstance(loopContent));
        
        MapElements.Add(newFileContent);
        
        if (guiNotificationAndMapRefreshWhenAdded) await StatusContext.ToastSuccess($"Added File - {possibleFile.Title}");
    }
    
    private async Task AddGeoJson(GeoJsonContent possibleGeoJson, MapElement? loopContent = null,
        bool guiNotificationAndMapRefreshWhenAdded = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (MapElements.Any(x => x.ContentId() == possibleGeoJson.ContentId))
        {
            await StatusContext.ToastWarning($"GeoJson {possibleGeoJson.Title} is already on the map");
            return;
        }
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var newGeoJsonItem = await MapElementListGeoJsonItem.CreateInstance(new GeoJsonContentActions(StatusContext),
            possibleGeoJson, MapElementSettings.CreateInstance(loopContent));
        
        MapElements.Add(newGeoJsonItem);
        
        if (guiNotificationAndMapRefreshWhenAdded) await StatusContext.ToastSuccess($"Added Point - {possibleGeoJson.Title}");
    }
    
    private async Task AddImage(ImageContent possibleImage, MapElement? loopContent = null,
        bool guiNotificationAndMapRefreshWhenAdded = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (MapElements.Any(x => x.ContentId() == possibleImage.ContentId))
        {
            await StatusContext.ToastWarning($"Image {possibleImage.Title} is already on the map");
            return;
        }
        
        if (!await possibleImage.HasValidLocation())
        {
            await StatusContext.ToastWarning($"Image {possibleImage.Title} doesn't have a valid location");
            return;
        }
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var newImageContent = await MapElementListImageItem.CreateInstance(new ImageContentActions(StatusContext),
            possibleImage, MapElementSettings.CreateInstance(loopContent));
        
        MapElements.Add(newImageContent);
        
        if (guiNotificationAndMapRefreshWhenAdded) await StatusContext.ToastSuccess($"Added Image - {possibleImage.Title}");
    }
    
    private async Task AddLine(LineContent possibleLine, MapElement? loopContent = null,
        bool guiNotificationAndMapRefreshWhenAdded = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (MapElements.Any(x => x.ContentId() == possibleLine.ContentId))
        {
            await StatusContext.ToastWarning($"Line {possibleLine.Title} is already on the map");
            return;
        }
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var newLineItem = await MapElementListLineItem.CreateInstance(new LineContentActions(StatusContext),
            possibleLine, MapElementSettings.CreateInstance(loopContent));
        
        MapElements.Add(newLineItem);
        
        if (guiNotificationAndMapRefreshWhenAdded) await StatusContext.ToastSuccess($"Added Point - {possibleLine.Title}");
    }
    
    private async Task AddPhoto(PhotoContent possiblePhoto, MapElement? loopContent = null,
        bool guiNotificationAndMapRefreshWhenAdded = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (MapElements.Any(x => x.ContentId() == possiblePhoto.ContentId))
        {
            await StatusContext.ToastWarning($"Photo {possiblePhoto.Title} is already on the map");
            return;
        }
        
        if (!await possiblePhoto.HasValidLocation())
        {
            await StatusContext.ToastWarning($"Photo {possiblePhoto.Title} doesn't have a valid location");
            return;
        }
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var newPhotoContent = await MapElementListPhotoItem.CreateInstance(new PhotoContentActions(StatusContext),
            possiblePhoto, MapElementSettings.CreateInstance(loopContent));
        
        MapElements.Add(newPhotoContent);
        
        if (guiNotificationAndMapRefreshWhenAdded) await StatusContext.ToastSuccess($"Added Photo - {possiblePhoto.Title}");
    }
    
    
    private async Task AddPoint(PointContentDto possiblePoint, MapElement? loopContent = null,
        bool guiNotificationAndMapRefreshWhenAdded = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (MapElements.Any(x => x.ContentId() == possiblePoint.ContentId))
        {
            await StatusContext.ToastWarning($"Point {possiblePoint.Title} is already on the map");
            return;
        }
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var newPointContent = await MapElementListPointItem.CreateInstance(new PointContentActions(StatusContext),
            possiblePoint, MapElementSettings.CreateInstance(loopContent));
        
        MapElements.Add(newPointContent);
        
        if (guiNotificationAndMapRefreshWhenAdded) await StatusContext.ToastSuccess($"Added Point - {possiblePoint.Title}");
    }
    
    private async Task AddPost(PostContent possiblePost, MapElement? loopContent = null,
        bool guiNotificationAndMapRefreshWhenAdded = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (MapElements.Any(x => x.ContentId() == possiblePost.ContentId))
        {
            await StatusContext.ToastWarning($"Post {possiblePost.Title} is already on the map");
            return;
        }
        
        if (!await possiblePost.HasValidLocation())
        {
            await StatusContext.ToastWarning($"Post {possiblePost.Title} doesn't have a valid location");
            return;
        }
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var newPostContent = await MapElementListPostItem.CreateInstance(new PostContentActions(StatusContext),
            possiblePost, MapElementSettings.CreateInstance(loopContent));
        
        MapElements.Add(newPostContent);
        
        if (guiNotificationAndMapRefreshWhenAdded) await StatusContext.ToastSuccess($"Added Post - {possiblePost.Title}");
    }
    
    private async Task AddVideo(VideoContent possibleVideo, MapElement? loopContent = null,
        bool guiNotificationAndMapRefreshWhenAdded = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (MapElements.Any(x => x.ContentId() == possibleVideo.ContentId))
        {
            await StatusContext.ToastWarning($"Video {possibleVideo.Title} is already on the map");
            return;
        }
        
        if (!await possibleVideo.HasValidLocation())
        {
            await StatusContext.ToastWarning($"Video {possibleVideo.Title} doesn't have a valid location");
            return;
        }
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var newVideoContent = await MapElementListVideoItem.CreateInstance(new VideoContentActions(StatusContext),
            possibleVideo, MapElementSettings.CreateInstance(loopContent));
        
        MapElements.Add(newVideoContent);
        
        if (guiNotificationAndMapRefreshWhenAdded) await StatusContext.ToastSuccess($"Added Video - {possibleVideo.Title}");
    }
    
    [NonBlockingCommand]
    public Task CloseAllPopups()
    {
        var jsRequest = new ExecuteJavaScript
            { JavaScriptToExecute = "closeAllPopups()", RequestTag = "Map Component Editor Close All Popups Command" };
        ToWebView.Enqueue(jsRequest);
        return Task.CompletedTask;
    }
    
    public static async Task<MapComponentEditorContext> CreateInstance(StatusControlContext? statusContext,
        MapComponent? mapComponent)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var factoryContext = statusContext ?? new StatusControlContext();
        var factoryMapIcons = await MapIconGenerator.SerializedMapIcons();
        
        await ThreadSwitcher.ResumeForegroundAsync();
        var factoryListSelection = await ContentListSelected<IMapElementListItem>.CreateInstance(factoryContext);
        var factoryMapList = new ObservableCollection<IMapElementListItem>();
        
        var newControl = new MapComponentEditorContext(factoryContext, factoryListSelection, factoryMapList,
            NewContentModels.InitializeMapComponent(mapComponent), factoryMapIcons);
        await newControl.LoadData(mapComponent);
        
        return newControl;
    }
    
    public MapComponentDto CurrentStateToContent()
    {
        var newEntry = MapComponent.CreateInstance();
        
        if (DbEntry.Id > 0)
        {
            newEntry.ContentId = DbEntry.ContentId;
            newEntry.CreatedOn = DbEntry.CreatedOn;
            newEntry.LastUpdatedOn = DateTime.Now;
            newEntry.LastUpdatedBy = CreatedUpdatedDisplay?.UpdatedByEntry.UserValue.TrimNullToEmpty() ?? string.Empty;
        }
        
        newEntry.Summary = SummaryEntry?.UserValue.TrimNullToEmpty() ?? string.Empty;
        newEntry.Title = TitleEntry?.UserValue.TrimNullToEmpty() ?? string.Empty;
        newEntry.CreatedBy = CreatedUpdatedDisplay?.CreatedByEntry.UserValue.TrimNullToEmpty() ?? string.Empty;
        newEntry.UpdateNotes = UpdateNotes?.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotesFormat = UpdateNotes?.UpdateNotesFormat.SelectedContentFormatAsString ?? string.Empty;
        
        var currentElementList = MapElements.ToList();
        var finalElementList = currentElementList.Select(x => new MapElement
        {
            MapComponentContentId = newEntry.ContentId,
            ElementContentId = x.ContentId() ?? Guid.Empty,
            IsFeaturedElement = x.ElementSettings.IsFeaturedElement,
            IncludeInDefaultView = x.ElementSettings.InInitialView,
            ShowDetailsDefault = x.ElementSettings.ShowInitialDetails
        }).ToList();
        
        return new MapComponentDto(newEntry, finalElementList);
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
        
        var existingListItemsMatchingNotification = new List<IMapElementListItem>();
        
        foreach (var loopItem in MapElements)
        {
            var id = loopItem.ContentId();
            if (id == null) continue;
            if (translatedMessage.ContentIds.Contains(id.Value))
                existingListItemsMatchingNotification.Add(loopItem);
        }
        
        if (!existingListItemsMatchingNotification.Any()) return;
        
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (translatedMessage.UpdateType == DataNotificationUpdateType.Delete)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            
            existingListItemsMatchingNotification.ForEach(x => MapElements.Remove(x));
            
            return;
        }
        
        var context = await Db.Context();
        var dbItems = new List<IContentId>();
        var toDelete = new List<IMapElementListItem>();
        
        foreach (var loopElements in existingListItemsMatchingNotification)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            
            switch (loopElements)
            {
                case MapElementListFileItem li:
                    var newFileDbItem = context.FileContents.SingleOrDefault(x => x.ContentId == li.ContentId());
                    if (newFileDbItem == null || !await newFileDbItem.HasValidLocation())
                    {
                        toDelete.Add(loopElements);
                    }
                    else
                    {
                        var newListItem =
                            await MapElementListFileItem.CreateInstance(new FileContentActions(StatusContext),
                                newFileDbItem, li.ElementSettings);
                        await ThreadSwitcher.ResumeForegroundAsync();
                        MapElements[MapElements.IndexOf(li)] = newListItem;
                    }
                    
                    break;
                case MapElementListGeoJsonItem li:
                    var newGeoJsonDbItem = context.GeoJsonContents.SingleOrDefault(x => x.ContentId == li.ContentId());
                    if (newGeoJsonDbItem == null)
                    {
                        toDelete.Add(loopElements);
                    }
                    else
                    {
                        var newListItem =
                            await MapElementListGeoJsonItem.CreateInstance(new GeoJsonContentActions(StatusContext),
                                newGeoJsonDbItem, li.ElementSettings);
                        await ThreadSwitcher.ResumeForegroundAsync();
                        MapElements[MapElements.IndexOf(li)] = newListItem;
                    }
                    
                    break;
                case MapElementListImageItem li:
                    var newImageDbItem = context.ImageContents.SingleOrDefault(x => x.ContentId == li.ContentId());
                    if (newImageDbItem == null || !await newImageDbItem.HasValidLocation())
                    {
                        toDelete.Add(loopElements);
                    }
                    else
                    {
                        var newListItem =
                            await MapElementListImageItem.CreateInstance(new ImageContentActions(StatusContext),
                                newImageDbItem, li.ElementSettings);
                        await ThreadSwitcher.ResumeForegroundAsync();
                        MapElements[MapElements.IndexOf(li)] = newListItem;
                    }
                    
                    break;
                case MapElementListLineItem li:
                    var newLineDbItem = context.LineContents.SingleOrDefault(x => x.ContentId == li.ContentId());
                    if (newLineDbItem == null)
                    {
                        toDelete.Add(loopElements);
                    }
                    else
                    {
                        var newListItem =
                            await MapElementListLineItem.CreateInstance(new LineContentActions(StatusContext),
                                newLineDbItem, li.ElementSettings);
                        await ThreadSwitcher.ResumeForegroundAsync();
                        MapElements[MapElements.IndexOf(li)] = newListItem;
                    }
                    
                    break;
                case MapElementListPhotoItem li:
                    var newPhotoDbItem = context.PhotoContents.SingleOrDefault(x => x.ContentId == li.ContentId());
                    if (newPhotoDbItem == null || !await newPhotoDbItem.HasValidLocation())
                    {
                        toDelete.Add(loopElements);
                    }
                    else
                    {
                        var newListItem =
                            await MapElementListPhotoItem.CreateInstance(new PhotoContentActions(StatusContext),
                                newPhotoDbItem, li.ElementSettings);
                        await ThreadSwitcher.ResumeForegroundAsync();
                        MapElements[MapElements.IndexOf(li)] = newListItem;
                    }
                    
                    break;
                case MapElementListPointItem li:
                    var newPointDbItem = context.PointContents.SingleOrDefault(x => x.ContentId == li.ContentId());
                    if (newPointDbItem == null)
                    {
                        toDelete.Add(loopElements);
                    }
                    else
                    {
                        var newListItem =
                            await MapElementListPointItem.CreateInstance(new PointContentActions(StatusContext),
                                await Db.PointContentDtoFromPoint(newPointDbItem, context), li.ElementSettings);
                        await ThreadSwitcher.ResumeForegroundAsync();
                        MapElements[MapElements.IndexOf(li)] = newListItem;
                    }
                    
                    break;
                case MapElementListPostItem li:
                    var newPostDbItem = context.PostContents.SingleOrDefault(x => x.ContentId == li.ContentId());
                    if (newPostDbItem == null || !await newPostDbItem.HasValidLocation())
                    {
                        toDelete.Add(loopElements);
                    }
                    else
                    {
                        var newListItem =
                            await MapElementListPostItem.CreateInstance(new PostContentActions(StatusContext),
                                newPostDbItem, li.ElementSettings);
                        await ThreadSwitcher.ResumeForegroundAsync();
                        MapElements[MapElements.IndexOf(li)] = newListItem;
                    }
                    
                    break;
                case MapElementListVideoItem li:
                    var newVideoDbItem = context.VideoContents.SingleOrDefault(x => x.ContentId == li.ContentId());
                    if (newVideoDbItem == null || !await newVideoDbItem.HasValidLocation())
                    {
                        toDelete.Add(loopElements);
                    }
                    else
                    {
                        var newListItem =
                            await MapElementListVideoItem.CreateInstance(new VideoContentActions(StatusContext),
                                newVideoDbItem, li.ElementSettings);
                        await ThreadSwitcher.ResumeForegroundAsync();
                        MapElements[MapElements.IndexOf(li)] = newListItem;
                    }
                    
                    break;
            }
        }
        
        foreach (var loopToDelete in toDelete)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            MapElements.Remove(loopToDelete);
        }
        
        StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }
    
    public async Task Edit(PointContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (content == null) return;
        
        var context = await Db.Context();
        
        var refreshedData = context.PointContents.SingleOrDefault(x => x.ContentId == content.ContentId);
        
        if (refreshedData == null)
            await StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");
        
        var newContentWindow = await PointContentEditorWindow.CreateInstance(refreshedData);
        
        await newContentWindow.PositionWindowAndShowOnUiThread();
    }
    
    public async Task Edit(LineContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (content == null) return;
        
        var context = await Db.Context();
        
        var refreshedData = context.PointContents.SingleOrDefault(x => x.ContentId == content.ContentId);
        
        if (refreshedData == null)
            await StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");
        
        var newContentWindow = await PointContentEditorWindow.CreateInstance(refreshedData);
        
        await newContentWindow.PositionWindowAndShowOnUiThread();
    }
    
    public async Task Edit(GeoJsonContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (content == null) return;
        
        var context = await Db.Context();
        
        var refreshedData = context.PointContents.SingleOrDefault(x => x.ContentId == content.ContentId);
        
        if (refreshedData == null)
            await StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");
        
        var newContentWindow = await PointContentEditorWindow.CreateInstance(refreshedData);
        
        await newContentWindow.PositionWindowAndShowOnUiThread();
    }
    
    public async Task<List<IMapElementListItem>> FilteredListItems()
    {
        var returnList = new List<IMapElementListItem>();
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var itemsView = CollectionViewSource.GetDefaultView(MapElements);
        
        var filter = itemsView.Filter;
        
        if (filter is null) return MapElements.ToList();
        
        foreach (var loopView in itemsView)
        {
            if (!filter(loopView)) continue;
            
            if (loopView is IMapElementListItem itemList) returnList.Add(itemList);
        }
        
        return returnList;
    }
    
    private async Task FilterList()
    {
        if (!MapElements.Any()) return;
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        if (string.IsNullOrWhiteSpace(UserFilterText))
        {
            ((CollectionView)CollectionViewSource.GetDefaultView(MapElements)).Filter = _ => true;
            return;
        }
        
        ((CollectionView)CollectionViewSource.GetDefaultView(MapElements)).Filter = x =>
            x is IMapElementListItem element &&
            element.Title.Contains(UserFilterText, StringComparison.CurrentCultureIgnoreCase);
    }
    
    [BlockingCommand]
    private async Task LinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (DbEntry.Id < 1)
        {
            await StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }
        
        var linkString = BracketCodeMapComponents.Create(DbEntry);
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        Clipboard.SetText(linkString);
        
        await StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }
    
    public async Task LoadData(MapComponent? toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;
        
        DbEntry = NewContentModels.InitializeMapComponent(toLoad);
        
        if (DbEntry.Id > 0)
        {
            var context = await Db.Context();
            DbElements = await context.MapComponentElements.Where(x => x.MapComponentContentId == DbEntry.ContentId)
                .ToListAsync();
        }
        
        TitleEntry = StringDataEntryContext.CreateInstance();
        TitleEntry.Title = "Title";
        TitleEntry.HelpText = "Title Text";
        TitleEntry.ReferenceValue = DbEntry.Title.TrimNullToEmpty();
        TitleEntry.UserValue = DbEntry.Title.TrimNullToEmpty();
        TitleEntry.ValidationFunctions = [CommonContentValidation.ValidateTitle];
        
        SummaryEntry = StringDataEntryContext.CreateInstance();
        SummaryEntry.Title = "Summary";
        SummaryEntry.HelpText = "A short text entry that will show in Search and short references to the content";
        SummaryEntry.ReferenceValue = DbEntry.Summary ?? string.Empty;
        SummaryEntry.UserValue = StringTools.NullToEmptyTrim(DbEntry.Summary);
        SummaryEntry.ValidationFunctions = [CommonContentValidation.ValidateSummary];
        
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        
        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
        
        var db = await Db.Context();
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        MapElements.Clear();
        
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        SuspendMapRefresh = true;
        
        foreach (var loopContent in DbElements)
        {
            var elementContent = await db.ContentFromContentId(loopContent.ElementContentId);
            
            switch (elementContent)
            {
                case FileContent p:
                    await AddFile(p, loopContent);
                    break;
                case GeoJsonContent gj:
                    await AddGeoJson(gj, loopContent);
                    break;
                case LineContent l:
                    await AddLine(l, loopContent);
                    break;
                case ImageContent p:
                    await AddImage(p, loopContent);
                    break;
                case PhotoContent p:
                    await AddPhoto(p, loopContent);
                    break;
                case PointContentDto p:
                    await AddPoint(p, loopContent);
                    break;
                case PostContent p:
                    await AddPost(p, loopContent);
                    break;
                case VideoContent p:
                    await AddVideo(p, loopContent);
                    break;
            }
        }
        
        SuspendMapRefresh = false;
        
        if (MapElements.Any()) await RefreshMapPreview();
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        await ListContextSortHelpers.SortList(ListSort.SortDescriptions(), MapElements);
        await FilterList();
        
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }
    
    private void MapElementsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!SuspendMapRefresh)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
                StatusContext.RunFireAndForgetNonBlockingTask(() => RefreshMapPreview(MapBounds));
            else
                StatusContext.RunFireAndForgetNonBlockingTask(() => RefreshMapPreview(MapBounds));
        }
    }
    
    private async Task MapMessageReceived(string mapMessage)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var parsedJson = JsonNode.Parse(mapMessage);
        
        if (parsedJson == null) return;
        
        var messageType = parsedJson["messageType"]?.ToString() ?? string.Empty;
        
        if (messageType == "mapBoundsChange")
            try
            {
                MapBounds = new SpatialBounds(parsedJson["bounds"]["_northEast"]["lat"].GetValue<double>(),
                    parsedJson["bounds"]["_northEast"]["lng"].GetValue<double>(),
                    parsedJson["bounds"]["_southWest"]["lat"].GetValue<double>(),
                    parsedJson["bounds"]["_southWest"]["lng"].GetValue<double>());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
    }
    
    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor.Enqueue(e);
    }
    
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        
        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
        
        if (e.PropertyName.Equals(nameof(UserFilterText)))
            StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }
    
    [BlockingCommand]
    private async Task OpenItemEditor(IMapElementListItem? toEdit)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (toEdit == null)
        {
            await StatusContext.ToastError("No Item to Edit?");
            return;
        }
        
        switch (toEdit)
        {
            case MapElementListPointItem p:
                await Edit(p.DbEntry.ToDbObject());
                break;
            case MapElementListLineItem l:
                await Edit(l.DbEntry);
                break;
            case MapElementListGeoJsonItem g:
                await Edit(g.DbEntry);
                break;
        }
    }
    
    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task PopupsForSelectedItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var bounds = SelectedListItems().Select(x => x.ContentId()).Where(x => x is not null).Select(x => x!.Value)
            .ToList();
        
        var popupData = new MapJsonFeatureListDto(bounds, "ShowPopupsFor");
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var serializedData = JsonSerializer.Serialize(popupData);
        
        ToWebView.Enqueue(new JsonData { Json = serializedData });
    }
    
    public Task ProcessFromWebView(FromWebViewMessage args)
    {
        if (!string.IsNullOrWhiteSpace(args.Message))
            StatusContext.RunFireAndForgetNonBlockingTask(async () => await MapMessageReceived(args.Message));
        return Task.CompletedTask;
    }
    
    [NonBlockingCommand]
    public async Task RefreshMapPreview(SpatialBounds? bounds = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var mapInformation =
            await MapCmsJson.ProcessContentToMapInformation(MapElements.Select(x => x.ContentId()!.Value).ToList());
        
        if (mapInformation.fileCopyList.Any())
        {
            var fileBuilder = new FileBuilder();
            fileBuilder.Copy.AddRange(mapInformation.fileCopyList.Select(x => new FileBuilderCopy(x)));
            
            ToWebView.Enqueue(fileBuilder);
        }
        
        ContentBounds = mapInformation.bounds.ToEnvelope();
        
        ToWebView.Enqueue(new JsonData
        {
            Json = await MapCmsJson.NewMapFeatureCollectionDtoSerialized(
                mapInformation.featureList, bounds ??
                                            mapInformation.bounds.ExpandToMinimumMeters(1000))
        });
    }
    
    [BlockingCommand]
    private async Task RemoveItem(IMapElementListItem? toRemove)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        
        if (toRemove == null)
        {
            await StatusContext.ToastError("No Item Selected?");
            return;
        }
        
        try
        {
            MapElements.Remove(toRemove);
        }
        catch (Exception e)
        {
            await StatusContext.ToastError($"Trouble Removing Map Element - {e.Message}");
        }
        
        await ThreadSwitcher.ResumeBackgroundAsync();
    }
    
    
    [NonBlockingCommand]
    public async Task RequestMapCenterOnAllItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (!MapElements.Any())
        {
            await StatusContext.ToastError("No Items?");
            return;
        }
        
        if (ContentBounds == null) return;
        
        await RequestMapCenterOnEnvelope(ContentBounds);
    }
    
    [NonBlockingCommand]
    public async Task RequestMapCenterOnContent(IContentListItem toCenter)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var centerData = new MapJsonFeatureDto(toCenter.ContentId()!.Value, "CenterFeatureRequest");
        var serializedData = JsonSerializer.Serialize(centerData);
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        ToWebView.Enqueue(new JsonData { Json = serializedData });
    }
    
    public async Task RequestMapCenterOnEnvelope(Envelope toCenter)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (toCenter is { Width: 0, Height: 0 })
        {
            await RequestMapCenterOnPoint(toCenter.MinX, toCenter.MinY);
            return;
        }
        
        var centerData = new MapJsonBoundsDto(
            new SpatialBounds(toCenter.MaxY, toCenter.MaxX, toCenter.MinY, toCenter.MinX).ExpandToMinimumMeters(1000),
            "CenterBoundingBoxRequest");
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var serializedData = JsonSerializer.Serialize(centerData);
        
        ToWebView.Enqueue(new JsonData { Json = serializedData });
    }
    
    [NonBlockingCommand]
    public async Task RequestMapCenterOnFilteredItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var filteredItems = await FilteredListItems();
        
        if (!filteredItems.Any())
        {
            await StatusContext.ToastError("No Visible Items?");
            return;
        }
        
        var bounds = MapCmsJson.GetBounds(filteredItems.Cast<IContentListItem>().ToList());
        
        await RequestMapCenterOnEnvelope(bounds);
    }
    
    public async Task RequestMapCenterOnPoint(double longitude, double latitude)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var centerData = new MapJsonCoordinateDto(latitude, longitude, "CenterCoordinateRequest");
        
        var serializedData = JsonSerializer.Serialize(centerData);
        
        ToWebView.Enqueue(new JsonData { Json = serializedData });
    }
    
    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task RequestMapCenterOnSelectedItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var bounds = MapCmsJson.GetBounds(SelectedListItems().Cast<IContentListItem>().ToList());
        
        await RequestMapCenterOnEnvelope(bounds);
    }
    
    [BlockingCommand]
    public async Task Save()
    {
        await SaveAndGenerateHtml(false);
    }
    
    [BlockingCommand]
    public async Task SaveAndClose()
    {
        await SaveAndGenerateHtml(true);
    }
    
    public async Task SaveAndGenerateHtml(bool closeAfterSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var (generationReturn, newContent) =
            await MapComponentGenerator.SaveAndGenerateData(CurrentStateToContent(), null,
                StatusContext.ProgressTracker());
        
        if (generationReturn.HasError || newContent == null)
        {
            await StatusContext.ShowMessageWithOkButton("Problem Saving and Generating Html",
                generationReturn.GenerationNote);
            return;
        }
        
        await LoadData(newContent.ToDbObject());
        
        if (closeAfterSave)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            RequestContentEditorWindowClose?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public List<IMapElementListItem> SelectedListItems()
    {
        return ListSelection.SelectedItems;
    }
    
    public static ColumnSortControlContext SortContextMapElementsDefault()
    {
        return new ColumnSortControlContext
        {
            Items =
            [
                new ColumnSortControlSortItem
                {
                    DisplayName = "Title",
                    ColumnName = "Title",
                    Order = 1,
                    DefaultSortDirection = ListSortDirection.Ascending
                },
                
                new ColumnSortControlSortItem
                {
                    DisplayName = "Type",
                    ColumnName = "Type",
                    DefaultSortDirection = ListSortDirection.Ascending
                }
            ]
        };
    }
    
    /// <summary>
    ///     Will add an item to the map if it is a point, line or geojson type - this does NOT refresh the map
    /// </summary>
    /// <param name="toAdd"></param>
    /// <returns></returns>
    private async Task TryAddSpatialType(Guid toAdd)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var db = await Db.Context();
        
        if (db.FileContents.Any(x => x.ContentId == toAdd))
        {
            await AddFile(await db.FileContents.SingleAsync(x => x.ContentId == toAdd))
                .ConfigureAwait(false);
            return;
        }
        
        if (db.GeoJsonContents.Any(x => x.ContentId == toAdd))
        {
            await AddGeoJson(await db.GeoJsonContents.SingleAsync(x => x.ContentId == toAdd)).ConfigureAwait(false);
            return;
        }
        
        if (db.ImageContents.Any(x => x.ContentId == toAdd))
        {
            await AddImage(await db.ImageContents.SingleAsync(x => x.ContentId == toAdd))
                .ConfigureAwait(false);
            return;
        }
        
        if (db.LineContents.Any(x => x.ContentId == toAdd))
        {
            await AddLine(await db.LineContents.SingleAsync(x => x.ContentId == toAdd)).ConfigureAwait(false);
            return;
        }
        
        if (db.PhotoContents.Any(x => x.ContentId == toAdd))
        {
            await AddPhoto(await db.PhotoContents.SingleAsync(x => x.ContentId == toAdd))
                .ConfigureAwait(false);
            return;
        }
        
        if (db.PointContents.Any(x => x.ContentId == toAdd))
        {
            await AddPoint((await Db.PointContentDto(toAdd))!)
                .ConfigureAwait(false);
            return;
        }
        
        if (db.PostContents.Any(x => x.ContentId == toAdd))
        {
            await AddPost(await db.PostContents.SingleAsync(x => x.ContentId == toAdd))
                .ConfigureAwait(false);
            return;
        }
        
        if (db.VideoContents.Any(x => x.ContentId == toAdd))
        {
            await AddVideo(await db.VideoContents.SingleAsync(x => x.ContentId == toAdd))
                .ConfigureAwait(false);
            return;
        }
        
        await StatusContext.ToastError("Item isn't a spatial type or isn't in the db?");
    }
    
    [BlockingCommand]
    public async Task UserGeoContentIdInputToMap()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (string.IsNullOrWhiteSpace(UserGeoContentInput))
        {
            await StatusContext.ToastError("Nothing to add?");
            return;
        }
        
        var codes = new List<Guid>();
        
        var regexObj = new Regex(@"(?:(\()|(\{))?\b[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\b(?(1)\))(?(2)\})",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
        var matchResult = regexObj.Match(UserGeoContentInput);
        while (matchResult.Success)
        {
            codes.Add(Guid.Parse(matchResult.Value));
            matchResult = matchResult.NextMatch();
        }
        
        codes.AddRange(BracketCodeCommon.BracketCodeContentIds(UserGeoContentInput));
        
        codes = codes.Distinct().ToList();
        
        if (!codes.Any())
        {
            await StatusContext.ToastError("No Content Ids found?");
            return;
        }
        
        SuspendMapRefresh = true;
        
        var db = await Db.Context();
        
        foreach (var loopCode in codes)
        {
            var possibleFile = await db.FileContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
            if (possibleFile != null)
            {
                await AddFile(possibleFile);
                continue;
            }
            
            var possibleGeoJson = await db.GeoJsonContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
            if (possibleGeoJson != null)
            {
                await AddGeoJson(possibleGeoJson);
                continue;
            }
            
            var possibleImage = await db.ImageContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
            if (possibleImage != null)
            {
                await AddImage(possibleImage);
                continue;
            }
            
            var possibleLine = await db.LineContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
            if (possibleLine != null)
            {
                await AddLine(possibleLine);
                continue;
            }
            
            var possiblePhoto = await db.PhotoContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
            if (possiblePhoto != null)
            {
                await AddPhoto(possiblePhoto);
                continue;
            }
            
            var possiblePoint = await db.PointContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
            if (possiblePoint != null)
            {
                var pointDto = (await Db.PointContentDto(possiblePoint.ContentId, db))!;
                await AddPoint(pointDto);
                continue;
            }
            
            var possiblePost = await db.PostContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
            if (possiblePost != null)
            {
                await AddPost(possiblePost);
                continue;
            }
            
            var possibleVideo = await db.VideoContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
            if (possibleVideo != null)
            {
                await AddVideo(possibleVideo);
                continue;
            }
            
            await StatusContext.ToastWarning(
                $"ContentId {loopCode} doesn't appear to be a valid GeoJson, Line, Photo or Point content for the map?");
        }
        
        await RefreshMapPreview();
        
        SuspendMapRefresh = false;
        
        UserGeoContentInput = string.Empty;
    }
}