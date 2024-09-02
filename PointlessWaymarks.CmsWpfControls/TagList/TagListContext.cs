using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Windows.Data;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;
using PointlessWaymarks.CmsWpfControls.GeoJsonContentEditor;
using PointlessWaymarks.CmsWpfControls.ImageContentEditor;
using PointlessWaymarks.CmsWpfControls.LineContentEditor;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.CmsWpfControls.NoteContentEditor;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.PostContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility.Excel;
using PointlessWaymarks.CmsWpfControls.VideoContentEditor;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.TagList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class TagListContext
{
    private TagListContext(StatusControlContext context, ObservableCollection<TagListListItem> itemCollection,
        bool loadInBackground = true)
    {
        StatusContext = context;
        CommonCommands = new CmsCommonCommands(StatusContext);

        BuildCommands();

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        Items = itemCollection;

        PropertyChanged += OnPropertyChanged;

        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public CmsCommonCommands CommonCommands { get; set; }
    public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }
    public List<TagItemContentInformation> DetailsList { get; set; } = [];
    public List<TagItemContentInformation>? DetailsSelectedItems { get; set; }
    public ObservableCollection<TagListListItem> Items { get; set; }
    public List<TagListListItem>? SelectedItems { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string UserFilterText { get; set; } = string.Empty;

    [BlockingCommand]
    public async Task AllDetailItemsToExcel()
    {
        await TagContentToExcel(DetailsList);
    }

    private async Task<string> ContentTypeString(dynamic content)
    {
        var contentTypeString = Db.ContentTypeDisplayString(content);

        if (contentTypeString == string.Empty)
        {
            Log.Error(new DataException("The Content Object was of Unknown Type"), "TagListContext Error");
            await StatusContext.ToastError("Unknown Content Type - Unusual Error...");
            return "Unknown";
        }

        return contentTypeString;
    }

    public static async Task<TagListContext> CreateInstance(StatusControlContext? statusContext, bool loadInBackground = true)
    {
        var factoryContext = await StatusControlContext.ResumeForegroundAsyncAndCreateInstance(statusContext);

        return new TagListContext(factoryContext, [], loadInBackground);
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs e)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var translatedMessage = DataNotifications.TranslateDataNotification(e.Message);

        if (translatedMessage.HasError)
        {
            Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}",
                translatedMessage.ErrorNote, StatusContext.StatusControlContextId);
            return;
        }

        if (translatedMessage.ContentType == DataNotificationContentType.TagExclusion)
        {
            //Tag Exclusions data notifications don't have enough information to do anything but process the whole list
            var currentExclusions = (await Db.TagExclusions()).Select(x => x.Tag).ToList();

            var items = Items.ToList();

            foreach (var loopItems in items)
                loopItems.IsExcludedTag = currentExclusions.Contains(loopItems.TagName);

            return;
        }

        switch (translatedMessage.UpdateType)
        {
            case DataNotificationUpdateType.LocalContent:
                return;
            case DataNotificationUpdateType.Delete:
            {
                var relatedEntries = ListItemsWithContentIds(translatedMessage.ContentIds);

                foreach (var loopEntries in relatedEntries)
                {
                    var contentToRemoveList = loopEntries.ContentInformation
                        .Where(x => translatedMessage.ContentIds.Contains(x.ContentId)).ToList();

                    var newContentList = loopEntries.ContentInformation.Except(contentToRemoveList).ToList();

                    if (!newContentList.Any())
                    {
                        await ThreadSwitcher.ResumeForegroundAsync();
                        Items.Remove(loopEntries);
                        await ThreadSwitcher.ResumeBackgroundAsync();
                        continue;
                    }

                    loopEntries.ContentInformation = newContentList;
                    loopEntries.ContentCount = newContentList.Count;
                }

                return;
            }
        }

        var db = await Db.Context();

        foreach (var loopContent in translatedMessage.ContentIds)
        {
            var content = await db.ContentFromContentId(loopContent);

            if (content == null) continue;

            var tags = Db.TagListParseToSlugs((ITag)content, false);

            var listContent = ListItemsWithContentIds(loopContent.AsList());

            var tagsToRemove = listContent.Select(x => x.TagName).Except(tags).ToList();
            var tagListEntriesToRemove = Items.Where(x => tagsToRemove.Contains(x.TagName)).ToList();

            foreach (var loopEntries in tagListEntriesToRemove)
            {
                var contentToRemoveList = loopEntries.ContentInformation
                    .Where(x => x.ContentId == loopContent).ToList();

                var newContentList = loopEntries.ContentInformation.Except(contentToRemoveList).ToList();

                if (!newContentList.Any())
                {
                    await ThreadSwitcher.ResumeForegroundAsync();
                    Items.Remove(loopEntries);
                    await ThreadSwitcher.ResumeBackgroundAsync();
                    continue;
                }

                loopEntries.ContentInformation = newContentList;
                loopEntries.ContentCount = newContentList.Count;
                UpdateDetails();
            }

            foreach (var loopTags in tags)
            {
                var possibleTagEntry = Items.SingleOrDefault(x => x.TagName == loopTags);
                var newContentEntry = new TagItemContentInformation
                {
                    ContentId = content.ContentId,
                    Title = content.Title,
                    Tags = content.Tags,
                    ContentType = await ContentTypeString(content)
                };

                if (possibleTagEntry == null)
                {
                    var toAdd = new TagListListItem
                    {
                        TagName = loopTags, ContentInformation = newContentEntry.AsList(), ContentCount = 1
                    };

                    await ThreadSwitcher.ResumeForegroundAsync();
                    Items.Add(toAdd);
                    await ThreadSwitcher.ResumeBackgroundAsync();
                }
                else
                {
                    var existingContentEntries = possibleTagEntry.ContentInformation
                        .Where(x => x.ContentId == newContentEntry.ContentId).ToList();
                    var adjustedContentEntries = possibleTagEntry.ContentInformation.Except(existingContentEntries)
                        .Concat(newContentEntry.AsList()).OrderBy(x => x.Title).ToList();

                    possibleTagEntry.ContentInformation = adjustedContentEntries;
                    possibleTagEntry.ContentCount = adjustedContentEntries.Count;
                    UpdateDetails();
                }
            }
        }
    }

    [BlockingCommand]
    public async Task EditContent(Guid contentId)
    {
        var db = await Db.Context();
        var content = await db.ContentFromContentId(contentId);

        if (content == null)
        {
            await StatusContext.ToastWarning("Nothing to edit?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        //!!Content Type List!!
        switch (content)
        {
            case FileContent c:
                (await FileContentEditorWindow.CreateInstance(c)).PositionWindowAndShow();
                break;
            case ImageContent c:
                (await ImageContentEditorWindow.CreateInstance(c)).PositionWindowAndShow();
                break;
            case GeoJsonContent c:
                (await GeoJsonContentEditorWindow.CreateInstance(c)).PositionWindowAndShow();
                break;
            case LineContent c:
                (await LineContentEditorWindow.CreateInstance(c)).PositionWindowAndShow();
                break;
            case LinkContent c:
                (await LinkContentEditorWindow.CreateInstance(c)).PositionWindowAndShow();
                break;
            case NoteContent c:
                (await NoteContentEditorWindow.CreateInstance(c)).PositionWindowAndShow();
                break;
            case PhotoContent c:
                (await PhotoContentEditorWindow.CreateInstance(c)).PositionWindowAndShow();
                break;
            case PointContent c:
                (await PointContentEditorWindow.CreateInstance(c)).PositionWindowAndShow();
                break;
            case PostContent c:
                (await PostContentEditorWindow.CreateInstance(c)).PositionWindowAndShow();
                break;
            case VideoContent c:
                (await VideoContentEditorWindow.CreateInstance(c)).PositionWindowAndShow();
                break;

            default:
                await StatusContext.ToastError("Content Type is Unknown?");
                break;
        }
    }

    private async Task FilterList()
    {
        if (!Items.Any()) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        ((CollectionView)CollectionViewSource.GetDefaultView(Items)).Filter = o =>
        {
            if (string.IsNullOrWhiteSpace(UserFilterText)) return true;

            var itemToFilter = (TagListListItem)o;

            return ListFilter(itemToFilter);
        };
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

    public bool ListFilter(TagListListItem toFilter)
    {
        if (string.IsNullOrWhiteSpace(UserFilterText)) return true;

        return toFilter.TagName.Contains(UserFilterText);
    }

    private List<TagListListItem> ListItemsWithContentIds(List<Guid>? contentIds)
    {
        var returnList = new List<TagListListItem>();

        if (contentIds == null || !contentIds.Any()) return returnList;

        foreach (var loopIds in contentIds)
            returnList.AddRange(Items.Where(x => x.ContentInformation.Select(y => y.ContentId).Contains(loopIds)));

        return returnList;
    }

    [BlockingCommand]
    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

        var allTags = await Db.TagSlugsAndContentList(true, false, StatusContext.ProgressTracker());

        var excludedTagSlugs = await Db.TagExclusionSlugs();

        var listItems = new List<TagListListItem>();

        foreach (var (tag, contentObjects) in allTags)
        {
            var toAdd = new TagListListItem
            {
                TagName = tag,
                ContentCount = contentObjects.Count,
                IsExcludedTag = excludedTagSlugs.Any(x => x == tag)
            };

            var contentDetails = new List<TagItemContentInformation>();

            foreach (var loopContent in contentObjects)
            {
                var detailToAdd = new TagItemContentInformation { ContentId = loopContent.ContentId };

                detailToAdd.ContentId = loopContent.ContentId;
                detailToAdd.Title = loopContent.Title;
                detailToAdd.ContentType = await ContentTypeString(loopContent);
                detailToAdd.Tags = loopContent.Tags;

                contentDetails.Add(detailToAdd);
            }

            toAdd.ContentInformation = contentDetails;
            listItems.Add(toAdd);
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        listItems.OrderBy(x => x.TagName).ToList().ForEach(x => Items.Add(x));

        await SortList("TagName");

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    [BlockingCommand]
    private async Task MakeExcludedTag(TagListListItem? arg)
    {
        if (arg == null) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var toExclude = arg.TagName;

        var currentEntries = await Db.TagExclusionSlugAndExclusions();

        //In this case the Exclusion exists and the GUI data is out-of-date
        if (currentEntries.Any(x => x.slug == toExclude))
        {
            await LoadData();
            return;
        }

        var saveResult = await TagExclusionGenerator.Save(new TagExclusion { Tag = toExclude });

        if (saveResult.generationReturn.HasError)
        {
            Log.ForContext("GenerationReturn", saveResult.generationReturn.SafeObjectDump())
                .ForContext("TagExclusion", saveResult.returnContent?.SafeObjectDump())
                .Error("Error Saving Tag Exclusion");
            await StatusContext.ShowMessageWithOkButton("Trouble Saving Tag Exclusion",
                $"Trouble saving Tag Exclusion - {saveResult.generationReturn.GenerationNote}");
            await LoadData();
            return;
        }

        await StatusContext.ToastSuccess($"Saved Tag Exclusion {toExclude}");
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor.Enqueue(e);
    }


    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(SelectedItems))
            UpdateDetails();

        if (e.PropertyName == nameof(UserFilterText))
            StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }

    [BlockingCommand]
    private async Task RemoveExcludedTag(TagListListItem? arg)
    {
        if (arg == null) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var toExclude = arg.TagName;

        var currentEntries = await Db.TagExclusionSlugAndExclusions();

        var toRemove = currentEntries.SingleOrDefault(x => x.slug == toExclude);

        //In this case the Exclusion doesn't exist and the GUI data is out-of-date
        if (toRemove.exclusion == null)
        {
            await LoadData();
            return;
        }

        await Db.DeleteTagExclusion(toRemove.exclusion.Id);

        await StatusContext.ToastSuccess($"Removed Tag Exclusion {toExclude}");
    }

    [BlockingCommand]
    public async Task SelectedDetailItemsToExcel()
    {
        await TagContentToExcel(DetailsSelectedItems);
    }

    [BlockingCommand]
    public async Task SelectedTagsToExcel()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems == null || !SelectedItems.Any())
        {
            await StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var tagsProjection = SelectedItems.Select(x => new { x.TagName, x.ContentCount }).Cast<object>().ToList();

        if (!tagsProjection.Any())
        {
            await StatusContext.ToastError("Nothing Selected?");
            return;
        }

        await ThreadSwitcher.ResumeBackgroundAsync();

        ExcelHelpers.ContentToExcelFileAsTable(tagsProjection, "Tags", progress: StatusContext.ProgressTracker());
    }

    private async Task SortList(string sortColumn)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var collectionView = (CollectionView)CollectionViewSource.GetDefaultView(Items);
        collectionView.SortDescriptions.Clear();

        if (string.IsNullOrWhiteSpace(sortColumn)) return;
        collectionView.SortDescriptions.Add(new SortDescription($"{sortColumn}", ListSortDirection.Ascending));
    }

    public async Task TagContentToExcel(List<TagItemContentInformation>? items)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (items == null || !items.Any())
        {
            await StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var allContentIds = items.OrderByDescending(x => x.Title).Select(x => x.ContentId).ToList();

        var db = await Db.Context();
        var content = await db.ContentFromContentIds(allContentIds);

        var toTransfer = content.Select(x => StaticValueInjecter.InjectFrom(new ContentCommonShell(), x)).ToList();

        ExcelHelpers.ContentToExcelFileAsTable(toTransfer, "TagDetails", progress: StatusContext.ProgressTracker());
    }

    private void UpdateDetails()
    {
        if (SelectedItems == null || !SelectedItems.Any())
        {
            DetailsList = [];
            return;
        }

        DetailsList = SelectedItems.SelectMany(x => x.ContentInformation).OrderBy(x => x.Title).ToList();
    }


    [BlockingCommand]
    public async Task VisibleTagsToExcel()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!Items.Any())
        {
            await StatusContext.ToastError("No Items?");
            return;
        }

        var tagsProjection = Items.Where(ListFilter).Select(x => new { x.TagName, x.ContentCount }).Cast<object>()
            .ToList();

        if (!tagsProjection.Any())
        {
            await StatusContext.ToastError("Nothing Visible?");
            return;
        }

        await ThreadSwitcher.ResumeBackgroundAsync();

        ExcelHelpers.ContentToExcelFileAsTable(tagsProjection, "Tags", progress: StatusContext.ProgressTracker());
    }
}