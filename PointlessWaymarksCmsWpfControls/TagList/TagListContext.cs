using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using AngleSharp.Dom;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.FileContentEditor;
using PointlessWaymarksCmsWpfControls.ImageContentEditor;
using PointlessWaymarksCmsWpfControls.LinkStreamEditor;
using PointlessWaymarksCmsWpfControls.NoteContentEditor;
using PointlessWaymarksCmsWpfControls.PhotoContentEditor;
using PointlessWaymarksCmsWpfControls.PostContentEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.TagList
{
    public class TagListContext : INotifyPropertyChanged
    {
        private List<TagItemContentInformation> _detailsList;
        private List<TagItemContentInformation> _detailsSelectedItems;
        private Command<Guid> _editContentCommand;
        private Command _importFromExcelCommand;
        private ObservableCollection<TagListListItem> _items;
        private Command _refreshDataCommand;
        private Command _selectedDetailItemsToExcelCommand;
        private List<TagListListItem> _selectedItems;

        private StatusControlContext _statusContext;
        private string _userFilterText;
        private Command<object> _visibleTagsToExcelCommand;

        public TagListContext(StatusControlContext context)
        {
            StatusContext = context ?? new StatusControlContext();

            RefreshDataCommand = new Command(() => StatusContext.RunBlockingTask(LoadData));

            SelectedDetailItemsToExcelCommand = new Command(async () => await TagContentToExcel(DetailsSelectedItems));
            AllDetailItemsToExcelCommand = new Command(async () => await TagContentToExcel(DetailsList));

            EditContentCommand = new Command<Guid>(x =>
                StatusContext.RunBlockingTask(async () => await EditContent(x)));
            VisibleTagsToExcelCommand = new Command<object>(x => StatusContext.RunBlockingTask(VisibleTagsToExcel));
            SelectedTagsToExcelCommand = new Command<object>(x => StatusContext.RunBlockingTask(SelectedTagsToExcel));
            ImportFromExcelCommand = new Command(() =>
                StatusContext.RunBlockingTask(async () => await ExcelHelpers.ImportFromExcel(StatusContext)));

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public Command AllDetailItemsToExcelCommand { get; set; }

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

        public Command ImportFromExcelCommand
        {
            get => _importFromExcelCommand;
            set
            {
                if (Equals(value, _importFromExcelCommand)) return;
                _importFromExcelCommand = value;
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

        public Command<object> SelectedTagsToExcelCommand { get; set; }

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

        public Command<object> VisibleTagsToExcelCommand
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
                    new FileContentEditorWindow(c).Show();
                    break;
                case ImageContent c:
                    new ImageContentEditorWindow(c).Show();
                    break;
                case NoteContent c:
                    new NoteContentEditorWindow(c).Show();
                    break;
                case PhotoContent c:
                    new PhotoContentEditorWindow(c).Show();
                    break;
                case PostContent c:
                    new PostContentEditorWindow(c).Show();
                    break;
                case LinkStream c:
                    new LinkStreamEditorWindow(c).Show();
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

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var allTags = await Db.TagAndContentList(true, StatusContext.ProgressTracker());

            var listItems = new List<TagListListItem>();

            foreach (var loopAllTags in allTags)
            {
                var toAdd = new TagListListItem
                {
                    TagName = loopAllTags.tag, ContentCount = loopAllTags.contentObjects.Count
                };

                var contentDetails = new List<TagItemContentInformation>();

                foreach (var loopContent in loopAllTags.contentObjects)
                {
                    var detailToAdd = new TagItemContentInformation {ContentId = loopContent.ContentId};

                    switch (loopContent)
                    {
                        case FileContent c:
                            detailToAdd.ContentId = c.ContentId;
                            detailToAdd.Title = c.Title;
                            detailToAdd.ContentType = "File";
                            detailToAdd.Tags = c.Tags;
                            break;
                        case ImageContent c:
                            detailToAdd.ContentId = c.ContentId;
                            detailToAdd.Title = c.Title;
                            detailToAdd.ContentType = "File";
                            detailToAdd.Tags = c.Tags;
                            break;
                        case NoteContent c:
                            detailToAdd.ContentId = c.ContentId;
                            detailToAdd.Title = c.Title;
                            detailToAdd.ContentType = "File";
                            detailToAdd.Tags = c.Tags;
                            break;
                        case PhotoContent c:
                            detailToAdd.ContentId = c.ContentId;
                            detailToAdd.Title = c.Title;
                            detailToAdd.ContentType = "File";
                            detailToAdd.Tags = c.Tags;
                            break;
                        case PostContent c:
                            detailToAdd.ContentId = c.ContentId;
                            detailToAdd.Title = c.Title;
                            detailToAdd.ContentType = "File";
                            detailToAdd.Tags = c.Tags;
                            break;
                        case LinkStream c:
                            detailToAdd.ContentId = c.ContentId;
                            detailToAdd.Title = c.Title;
                            detailToAdd.ContentType = "File";
                            detailToAdd.Tags = c.Tags;
                            break;
                        default:
                            StatusContext.ToastError("Unknown Content Type - Unusual Error...");
                            await EventLogContext.TryWriteExceptionToLog(
                                new DataException("The Content Object was of Unknown Type"), "TagListContent Load", "");
                            break;
                    }

                    contentDetails.Add(detailToAdd);
                }

                toAdd.ContentInformation = contentDetails;
                listItems.Add(toAdd);
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            Items ??= new ObservableCollection<TagListListItem>();

            Items.Clear();

            listItems.OrderBy(x => x.TagName).ToList().ForEach(x => Items.Add(x));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task SelectedTagsToExcel()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems == null || !SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var tagsProjection = SelectedItems.Select(x => new {x.TagName, x.ContentCount,}).Cast<object>().ToList();

            if (!tagsProjection.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            ExcelHelpers.ContentToExcelFileAsTable(tagsProjection, "Tags");
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
            var content = db.ContentFromContentIds(allContentIds);

            var toTransfer = content.Select(x => StaticValueInjecter.InjectFrom(new ContentCommonShell(), x)).ToList();

            ExcelHelpers.ContentToExcelFileAsTable(toTransfer, "TagDetails");
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

            var tagsProjection = Items.Where(ListFilter).Select(x => new {x.TagName, x.ContentCount,}).Cast<object>()
                .ToList();

            if (!tagsProjection.Any())
            {
                StatusContext.ToastError("Nothing Visible?");
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            ExcelHelpers.ContentToExcelFileAsTable(tagsProjection, "Tags");
        }
    }
}