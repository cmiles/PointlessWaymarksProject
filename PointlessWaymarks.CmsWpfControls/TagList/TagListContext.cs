using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using JetBrains.Annotations;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;
using PointlessWaymarks.CmsWpfControls.ImageContentEditor;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.CmsWpfControls.NoteContentEditor;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.PostContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.TagList
{
    public class TagListContext : INotifyPropertyChanged
    {
        private Command _allDetailItemsToExcelCommand;
        private DataNotificationsWorkQueue _dataNotificationsProcessor;
        private List<TagItemContentInformation> _detailsList;
        private List<TagItemContentInformation> _detailsSelectedItems;
        private Command<Guid> _editContentCommand;
        private Command _importFromExcelFileCommand;
        private Command _importFromOpenExcelInstanceCommand;
        private ObservableCollection<TagListListItem> _items;
        private Command<TagListListItem> _makeExcludedTagCommand;
        private Command _refreshDataCommand;
        private Command<TagListListItem> _removeExcludedTagCommand;
        private Command _selectedDetailItemsToExcelCommand;
        private List<TagListListItem> _selectedItems;

        private StatusControlContext _statusContext;
        private string _userFilterText;
        private Command _visibleTagsToExcelCommand;

        public TagListContext(StatusControlContext context)
        {
            StatusContext = context ?? new StatusControlContext();

            DataNotificationsProcessor = new DataNotificationsWorkQueue {Processor = DataNotificationReceived};

            RefreshDataCommand = StatusContext.RunBlockingTaskCommand(LoadData);

            SelectedDetailItemsToExcelCommand =
                StatusContext.RunBlockingTaskCommand(async () => await TagContentToExcel(DetailsSelectedItems));
            AllDetailItemsToExcelCommand =
                StatusContext.RunBlockingTaskCommand(async () => await TagContentToExcel(DetailsList));

            EditContentCommand = StatusContext.RunNonBlockingTaskCommand<Guid>(async x => await EditContent(x));
            VisibleTagsToExcelCommand = StatusContext.RunBlockingTaskCommand(VisibleTagsToExcel);
            SelectedTagsToExcelCommand = StatusContext.RunBlockingTaskCommand(SelectedTagsToExcel);

            MakeExcludedTagCommand = StatusContext.RunBlockingTaskCommand<TagListListItem>(MakeExcludedTag);
            RemoveExcludedTagCommand = StatusContext.RunBlockingTaskCommand<TagListListItem>(RemoveExcludedTag);

            ImportFromExcelFileCommand =
                StatusContext.RunBlockingTaskCommand(async () => await ExcelHelpers.ImportFromExcelFile(StatusContext));
            ImportFromOpenExcelInstanceCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await ExcelHelpers.ImportFromOpenExcelInstance(StatusContext));

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public Command AllDetailItemsToExcelCommand
        {
            get => _allDetailItemsToExcelCommand;
            set
            {
                if (Equals(value, _allDetailItemsToExcelCommand)) return;
                _allDetailItemsToExcelCommand = value;
                OnPropertyChanged();
            }
        }

        public DataNotificationsWorkQueue DataNotificationsProcessor
        {
            get => _dataNotificationsProcessor;
            set
            {
                if (Equals(value, _dataNotificationsProcessor)) return;
                _dataNotificationsProcessor = value;
                OnPropertyChanged();
            }
        }

        public List<TagItemContentInformation> DetailsList
        {
            get => _detailsList;
            set
            {
                if (Equals(value, _detailsList)) return;
                _detailsList = value;
                OnPropertyChanged();
            }
        }

        public List<TagItemContentInformation> DetailsSelectedItems
        {
            get => _detailsSelectedItems;
            set
            {
                if (Equals(value, _detailsSelectedItems)) return;
                _detailsSelectedItems = value;
                OnPropertyChanged();
            }
        }

        public Command<Guid> EditContentCommand
        {
            get => _editContentCommand;
            set
            {
                if (Equals(value, _editContentCommand)) return;
                _editContentCommand = value;
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

        public ObservableCollection<TagListListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public Command<TagListListItem> MakeExcludedTagCommand
        {
            get => _makeExcludedTagCommand;
            set
            {
                if (Equals(value, _makeExcludedTagCommand)) return;
                _makeExcludedTagCommand = value;
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

        public Command<TagListListItem> RemoveExcludedTagCommand
        {
            get => _removeExcludedTagCommand;
            set
            {
                if (Equals(value, _removeExcludedTagCommand)) return;
                _removeExcludedTagCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SelectedDetailItemsToExcelCommand
        {
            get => _selectedDetailItemsToExcelCommand;
            set
            {
                if (Equals(value, _selectedDetailItemsToExcelCommand)) return;
                _selectedDetailItemsToExcelCommand = value;
                OnPropertyChanged();
            }
        }

        public List<TagListListItem> SelectedItems
        {
            get => _selectedItems;
            set
            {
                if (Equals(value, _selectedItems)) return;
                _selectedItems = value;
                OnPropertyChanged();

                UpdateDetails();
            }
        }

        public Command SelectedTagsToExcelCommand { get; set; }

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

        public Command VisibleTagsToExcelCommand
        {
            get => _visibleTagsToExcelCommand;
            set
            {
                if (Equals(value, _visibleTagsToExcelCommand)) return;
                _visibleTagsToExcelCommand = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string ContentTypeString(dynamic content)
        {
            var contentTypeString = Db.ContentTypeString(content);

            if (contentTypeString == string.Empty)
            {
                Log.Error(new DataException("The Content Object was of Unknown Type"), "TagListContext Error");
                StatusContext.ToastError("Unknown Content Type - Unusual Error...");
                return "Unknown";
            }

            return contentTypeString;
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
                //Tag Exclusions data notifications don't enough information to do anything but process the whole list                
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
                var tags = Db.TagListParseToSlugs((ITag) content, false);

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
                        ContentType = ContentTypeString(content)
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

        public async Task EditContent(Guid contentId)
        {
            var db = await Db.Context();
            var content = await db.ContentFromContentId(contentId);

            if (content == null)
            {
                StatusContext.ToastWarning("Nothing to edit?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            switch (content)
            {
                case FileContent c:
                    new FileContentEditorWindow(c).PositionWindowAndShow();
                    break;
                case ImageContent c:
                    new ImageContentEditorWindow(c).PositionWindowAndShow();
                    break;
                case NoteContent c:
                    new NoteContentEditorWindow(c).PositionWindowAndShow();
                    break;
                case PhotoContent c:
                    new PhotoContentEditorWindow(c).PositionWindowAndShow();
                    break;
                case PostContent c:
                    new PostContentEditorWindow(c).PositionWindowAndShow();
                    break;
                case LinkContent c:
                    new LinkContentEditorWindow(c).PositionWindowAndShow();
                    break;
                default:
                    StatusContext.ToastError("Content Type is Unknown?");
                    break;
            }
        }

        private async Task FilterList()
        {
            if (Items == null || !Items.Any()) return;

            await ThreadSwitcher.ResumeForegroundAsync();

            ((CollectionView) CollectionViewSource.GetDefaultView(Items)).Filter = o =>
            {
                if (string.IsNullOrWhiteSpace(UserFilterText)) return true;

                var itemToFilter = (TagListListItem) o;

                return ListFilter(itemToFilter);
            };
        }

        public bool ListFilter(TagListListItem toFilter)
        {
            if (string.IsNullOrWhiteSpace(UserFilterText)) return true;

            return toFilter.TagName.Contains(UserFilterText);
        }

        private List<TagListListItem> ListItemsWithContentIds(List<Guid> contentIds)
        {
            var returnList = new List<TagListListItem>();

            if (contentIds == null || !contentIds.Any()) return returnList;

            foreach (var loopIds in contentIds)
                returnList.AddRange(Items.Where(x => x.ContentInformation.Select(y => y.ContentId).Contains(loopIds)));

            return returnList;
        }

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
                    var detailToAdd = new TagItemContentInformation {ContentId = loopContent.ContentId};

                    detailToAdd.ContentId = loopContent.ContentId;
                    detailToAdd.Title = loopContent.Title;
                    detailToAdd.ContentType = ContentTypeString(loopContent);
                    detailToAdd.Tags = loopContent.Tags;

                    contentDetails.Add(detailToAdd);
                }

                toAdd.ContentInformation = contentDetails;
                listItems.Add(toAdd);
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            Items ??= new ObservableCollection<TagListListItem>();

            Items.Clear();

            listItems.OrderBy(x => x.TagName).ToList().ForEach(x => Items.Add(x));

            await SortList("TagName");

            DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
        }

        private async Task MakeExcludedTag(TagListListItem arg)
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

            var saveResult = await TagExclusionGenerator.Save(new TagExclusion {Tag = toExclude});

            if (saveResult.generationReturn.HasError)
            {
                Log.ForContext("GenerationReturn", saveResult.generationReturn.SafeObjectDump())
                    .ForContext("TagExclusion", saveResult.returnContent.SafeObjectDump())
                    .Error("Error Saving Tag Exclusion");
                await StatusContext.ShowMessageWithOkButton("Trouble Saving Tag Exclusion",
                    $"Trouble saving Tag Exclusion - {saveResult.generationReturn.GenerationNote}");
                await LoadData();
                return;
            }

            StatusContext.ToastSuccess($"Saved Tag Exclusion {toExclude}");
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

        private async Task RemoveExcludedTag(TagListListItem arg)
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

            StatusContext.ToastSuccess($"Removed Tag Exclusion {toExclude}");
        }

        public async Task SelectedTagsToExcel()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems == null || !SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var tagsProjection = SelectedItems.Select(x => new {x.TagName, x.ContentCount}).Cast<object>().ToList();

            if (!tagsProjection.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            ExcelHelpers.ContentToExcelFileAsTable(tagsProjection, "Tags", progress: StatusContext?.ProgressTracker());
        }

        private async Task SortList(string sortColumn)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var collectionView = (CollectionView) CollectionViewSource.GetDefaultView(Items);
            collectionView.SortDescriptions.Clear();

            if (string.IsNullOrWhiteSpace(sortColumn)) return;
            collectionView.SortDescriptions.Add(new SortDescription($"{sortColumn}", ListSortDirection.Ascending));
        }

        public async Task TagContentToExcel(List<TagItemContentInformation> items)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (items == null || !items.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var allContentIds = items.OrderByDescending(x => x.Title).Select(x => x.ContentId).ToList();

            var db = await Db.Context();
            var content = await db.ContentFromContentIds(allContentIds);

            var toTransfer = content.Select(x => StaticValueInjecter.InjectFrom(new ContentCommonShell(), x)).ToList();

            ExcelHelpers.ContentToExcelFileAsTable(toTransfer, "TagDetails",
                progress: StatusContext?.ProgressTracker());
        }

        private void UpdateDetails()
        {
            if (SelectedItems == null || !SelectedItems.Any())
            {
                DetailsList = new List<TagItemContentInformation>();
                return;
            }

            DetailsList = SelectedItems.SelectMany(x => x.ContentInformation).OrderBy(x => x.Title).ToList();
        }

        public async Task VisibleTagsToExcel()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (Items == null || !Items.Any())
            {
                StatusContext.ToastError("No Items?");
                return;
            }

            var tagsProjection = Items.Where(ListFilter).Select(x => new {x.TagName, x.ContentCount}).Cast<object>()
                .ToList();

            if (!tagsProjection.Any())
            {
                StatusContext.ToastError("Nothing Visible?");
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            ExcelHelpers.ContentToExcelFileAsTable(tagsProjection, "Tags", progress: StatusContext?.ProgressTracker());
        }
    }
}