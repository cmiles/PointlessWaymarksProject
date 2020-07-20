using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
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
        private TagListItemDetailDisplay _detailDisplay;
        private Command<object> _editContentCommand;
        private ObservableCollection<TagListListItem> _items;
        private Command _refreshDataCommand;
        private Command<TagListListItem> _showDetailsCommand;
        private Command<TagListListItem> _singleTagContentToExcelCommand;

        private StatusControlContext _statusContext;
        private Command _tagDetailRemoveCommand;
        private Command _tagDetailRenameCommand;
        private Command _tagsToClipboardCommand;

        public TagListContext(StatusControlContext context)
        {
            StatusContext = context ?? new StatusControlContext();

            RefreshDataCommand = new Command(() => StatusContext.RunBlockingTask(LoadData));
            TagDetailRenameCommand = new Command(() => StatusContext.RunBlockingTask(async () =>
            {
                await RenameTag();
                await LoadData();
            }));

            TagDetailRemoveCommand = new Command(() => StatusContext.RunBlockingTask(async () =>
            {
                await RemoveTag();
                await LoadData();
            }));

            TagsToClipboardCommand = new Command(() => StatusContext.RunBlockingTask(TagsToClipboard));
            ShowDetailsCommand = new Command<TagListListItem>(x =>
                StatusContext.RunBlockingTask(async () => await ShowDetails(x)));
            SingleTagContentToExcelCommand = new Command<TagListListItem>(x =>
                StatusContext.RunBlockingTask(async () => await SingleTagContentToExcel(x)));
            EditContentCommand = new Command<object>(x =>
                StatusContext.RunBlockingTask(async () => await EditContent(x)));

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public TagListItemDetailDisplay DetailDisplay
        {
            get => _detailDisplay;
            set
            {
                if (Equals(value, _detailDisplay)) return;
                _detailDisplay = value;
                OnPropertyChanged();
            }
        }

        public Command<object> EditContentCommand
        {
            get => _editContentCommand;
            set
            {
                if (Equals(value, _editContentCommand)) return;
                _editContentCommand = value;
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

        public Command<TagListListItem> ShowDetailsCommand
        {
            get => _showDetailsCommand;
            set
            {
                if (Equals(value, _showDetailsCommand)) return;
                _showDetailsCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<TagListListItem> SingleTagContentToExcelCommand
        {
            get => _singleTagContentToExcelCommand;
            set
            {
                if (Equals(value, _singleTagContentToExcelCommand)) return;
                _singleTagContentToExcelCommand = value;
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

        public Command TagDetailRemoveCommand
        {
            get => _tagDetailRemoveCommand;
            set
            {
                if (Equals(value, _tagDetailRemoveCommand)) return;
                _tagDetailRemoveCommand = value;
                OnPropertyChanged();
            }
        }

        public Command TagDetailRenameCommand
        {
            get => _tagDetailRenameCommand;
            set
            {
                if (Equals(value, _tagDetailRenameCommand)) return;
                _tagDetailRenameCommand = value;
                OnPropertyChanged();
            }
        }

        public Command TagsToClipboardCommand
        {
            get => _tagsToClipboardCommand;
            set
            {
                if (Equals(value, _tagsToClipboardCommand)) return;
                _tagsToClipboardCommand = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string AddTagAndCreateTagString(List<string> currentTags, string toAdd)
        {
            return Db.TagListJoin(AddTagListItem(currentTags, toAdd));
        }

        public List<string> AddTagListItem(List<string> currentTags, string toAdd)
        {
            if (currentTags == null || !currentTags.Any()) return new List<string>();

            if (string.IsNullOrWhiteSpace(toAdd)) return Db.TagListCleanup(currentTags);

            var cleanedToAdd = Db.TagListItemCleanup(toAdd);

            currentTags.Add(cleanedToAdd);

            return Db.TagListCleanup(currentTags);
        }

        public async Task EditContent(object content)
        {
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

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var allTags = await Db.TagAndContentList(true, StatusContext.ProgressTracker());
            var listItems = allTags.OrderBy(x => x.tag).Select(x => new TagListListItem
            {
                TagName = x.tag,
                ContentIds = x.contentObjects.Cast<IContentId>().Select(y => y.ContentId).ToList(),
                ContentObjects = x.contentObjects,
                ContentCount = x.contentObjects.Count
            }).ToList();

            await ThreadSwitcher.ResumeForegroundAsync();

            Items ??= new ObservableCollection<TagListListItem>();

            Items.Clear();

            listItems.ForEach(x => Items.Add(x));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task RemoveTag()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var details = DetailDisplay;

            if (details == null || string.IsNullOrWhiteSpace(details.UserNewTagName))
            {
                StatusContext.ToastError("Nothing to rename?");
                return;
            }

            if (string.IsNullOrWhiteSpace(details.UserNewTagName))
            {
                StatusContext.ToastError("Can not rename to a blank tag - maybe delete the tag?");
                return;
            }

            if (details.ContentList == null || details.ContentList.Count < 1)
            {
                StatusContext.ToastError("No Content with this Tag?");
                return;
            }

            var db = await Db.Context();

            void updateVersionAndUpdatedByAndOn(object updateObject)
            {
                var toUpdate = (dynamic) updateObject;
                toUpdate.ContentVersion = DateTime.Now.ToUniversalTime();
                toUpdate.LastUpdatedOn = DateTime.Now;
                toUpdate.LastUpdatedBy = string.IsNullOrWhiteSpace(toUpdate.LastUpdatedBy)
                    ? toUpdate.CreatedBy
                    : toUpdate.LastUpdatedBy;
            }


            foreach (var loopContent in details.ContentList)
            {
                List<string> oldTagList;
                string newTagListString;
                string oldTagListString;

                switch (loopContent.Content)
                {
                    case FileContent c:
                        var fileContent = db.FileContents.SingleOrDefault(x => x.ContentId == c.ContentId);
                        if (fileContent == null) continue;

                        oldTagList = Db.TagListParse(fileContent.Tags);
                        oldTagListString = Db.TagListJoin(oldTagList);
                        newTagListString = RemoveTagAndCreateTagString(oldTagList, details.ListItem.TagName);

                        if (oldTagListString != newTagListString)
                        {
                            updateVersionAndUpdatedByAndOn(fileContent);
                            fileContent.Tags = newTagListString;
                        }

                        await Db.SaveFileContent(fileContent);

                        break;
                    case ImageContent c:
                        var imageContent = db.ImageContents.SingleOrDefault(x => x.ContentId == c.ContentId);
                        if (imageContent == null) continue;

                        oldTagList = Db.TagListParse(imageContent.Tags);
                        oldTagListString = Db.TagListJoin(oldTagList);
                        newTagListString = RemoveTagAndCreateTagString(oldTagList, details.ListItem.TagName);

                        if (oldTagListString != newTagListString)
                        {
                            updateVersionAndUpdatedByAndOn(imageContent);
                            imageContent.Tags = newTagListString;
                        }

                        await Db.SaveImageContent(imageContent);
                        break;
                    case NoteContent c:
                        var noteContent = db.NoteContents.SingleOrDefault(x => x.ContentId == c.ContentId);
                        if (noteContent == null) continue;

                        oldTagList = Db.TagListParse(noteContent.Tags);
                        oldTagListString = Db.TagListJoin(oldTagList);
                        newTagListString = RemoveTagAndCreateTagString(oldTagList, details.ListItem.TagName);

                        if (oldTagListString != newTagListString)
                        {
                            updateVersionAndUpdatedByAndOn(noteContent);
                            noteContent.Tags = newTagListString;
                        }

                        await Db.SaveNoteContent(noteContent);
                        break;
                    case PhotoContent c:
                        var photoContent = db.PhotoContents.SingleOrDefault(x => x.ContentId == c.ContentId);
                        if (photoContent == null) continue;

                        oldTagList = Db.TagListParse(photoContent.Tags);
                        oldTagListString = Db.TagListJoin(oldTagList);
                        newTagListString = RemoveTagAndCreateTagString(oldTagList, details.ListItem.TagName);

                        if (oldTagListString != newTagListString)
                        {
                            updateVersionAndUpdatedByAndOn(photoContent);
                            photoContent.Tags = newTagListString;
                        }

                        await Db.SavePhotoContent(photoContent);
                        break;
                    case PostContent c:
                        var postContent = db.PostContents.SingleOrDefault(x => x.ContentId == c.ContentId);
                        if (postContent == null) continue;

                        oldTagList = Db.TagListParse(postContent.Tags);
                        oldTagListString = Db.TagListJoin(oldTagList);
                        newTagListString = RemoveTagAndCreateTagString(oldTagList, details.ListItem.TagName);

                        if (oldTagListString != newTagListString)
                        {
                            updateVersionAndUpdatedByAndOn(postContent);
                            postContent.Tags = newTagListString;
                        }

                        await Db.SavePostContent(postContent);
                        break;
                    case LinkStream c:
                        var linkStreamContent = db.LinkStreams.SingleOrDefault(x => x.ContentId == c.ContentId);
                        if (linkStreamContent == null) continue;

                        oldTagList = Db.TagListParse(linkStreamContent.Tags);
                        oldTagListString = Db.TagListJoin(oldTagList);
                        newTagListString = RemoveTagAndCreateTagString(oldTagList, details.ListItem.TagName);

                        if (oldTagListString != newTagListString)
                        {
                            updateVersionAndUpdatedByAndOn(linkStreamContent);
                            linkStreamContent.Tags = newTagListString;
                        }

                        await Db.SaveLinkStream(linkStreamContent);
                        break;
                    default:
                        StatusContext.ToastError("Content Type is Unknown?");
                        break;
                }
            }
        }

        public string RemoveTagAndCreateTagString(List<string> currentTags, string toRemove)
        {
            return Db.TagListJoin(RemoveTagListItem(currentTags, toRemove));
        }

        public List<string> RemoveTagListItem(List<string> currentTags, string toRemove)
        {
            if (currentTags == null || !currentTags.Any()) return new List<string>();

            if (string.IsNullOrWhiteSpace(toRemove)) return Db.TagListCleanup(currentTags);

            var cleanedToRemove = Db.TagListItemCleanup(toRemove);

            var cleanedList = Db.TagListCleanup(currentTags);

            if (cleanedList.Contains(cleanedToRemove)) cleanedList.Remove(cleanedToRemove);

            return cleanedList;
        }

        public async Task RenameTag()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var details = DetailDisplay;

            if (details == null || string.IsNullOrWhiteSpace(details.UserNewTagName))
            {
                StatusContext.ToastError("Nothing to rename?");
                return;
            }

            if (string.IsNullOrWhiteSpace(details.UserNewTagName))
            {
                StatusContext.ToastError("Can not rename to a blank tag - maybe delete the tag?");
                return;
            }

            if (details.ContentList == null || details.ContentList.Count < 1)
            {
                StatusContext.ToastError("No Content with this Tag?");
                return;
            }

            var db = await Db.Context();

            void updateVersionAndUpdatedByAndOn(object updateObject)
            {
                var toUpdate = (dynamic) updateObject;
                toUpdate.ContentVersion = DateTime.Now.ToUniversalTime();
                toUpdate.LastUpdatedOn = DateTime.Now;
                toUpdate.LastUpdatedBy = string.IsNullOrWhiteSpace(toUpdate.LastUpdatedBy)
                    ? toUpdate.CreatedBy
                    : toUpdate.LastUpdatedBy;
            }


            foreach (var loopContent in details.ContentList)
            {
                List<string> oldTagList;
                string newTagListString;
                string oldTagListString;

                switch (loopContent.Content)
                {
                    case FileContent c:
                        var fileContent = db.FileContents.SingleOrDefault(x => x.ContentId == c.ContentId);
                        if (fileContent == null) continue;

                        oldTagList = Db.TagListParse(fileContent.Tags);
                        oldTagListString = Db.TagListJoin(oldTagList);
                        newTagListString = RenameTagAndCreateTagString(oldTagList, details.ListItem.TagName,
                            details.UserNewTagName);

                        if (oldTagListString != newTagListString)
                        {
                            updateVersionAndUpdatedByAndOn(fileContent);
                            fileContent.Tags = newTagListString;
                        }

                        await Db.SaveFileContent(fileContent);

                        break;
                    case ImageContent c:
                        var imageContent = db.ImageContents.SingleOrDefault(x => x.ContentId == c.ContentId);
                        if (imageContent == null) continue;

                        oldTagList = Db.TagListParse(imageContent.Tags);
                        oldTagListString = Db.TagListJoin(oldTagList);
                        newTagListString = RenameTagAndCreateTagString(oldTagList, details.ListItem.TagName,
                            details.UserNewTagName);

                        if (oldTagListString != newTagListString)
                        {
                            updateVersionAndUpdatedByAndOn(imageContent);
                            imageContent.Tags = newTagListString;
                        }

                        await Db.SaveImageContent(imageContent);
                        break;
                    case NoteContent c:
                        var noteContent = db.NoteContents.SingleOrDefault(x => x.ContentId == c.ContentId);
                        if (noteContent == null) continue;

                        oldTagList = Db.TagListParse(noteContent.Tags);
                        oldTagListString = Db.TagListJoin(oldTagList);
                        newTagListString = RenameTagAndCreateTagString(oldTagList, details.ListItem.TagName,
                            details.UserNewTagName);

                        if (oldTagListString != newTagListString)
                        {
                            updateVersionAndUpdatedByAndOn(noteContent);
                            noteContent.Tags = newTagListString;
                        }

                        await Db.SaveNoteContent(noteContent);
                        break;
                    case PhotoContent c:
                        var photoContent = db.PhotoContents.SingleOrDefault(x => x.ContentId == c.ContentId);
                        if (photoContent == null) continue;

                        oldTagList = Db.TagListParse(photoContent.Tags);
                        oldTagListString = Db.TagListJoin(oldTagList);
                        newTagListString = RenameTagAndCreateTagString(oldTagList, details.ListItem.TagName,
                            details.UserNewTagName);

                        if (oldTagListString != newTagListString)
                        {
                            updateVersionAndUpdatedByAndOn(photoContent);
                            photoContent.Tags = newTagListString;
                        }

                        await Db.SavePhotoContent(photoContent);
                        break;
                    case PostContent c:
                        var postContent = db.PostContents.SingleOrDefault(x => x.ContentId == c.ContentId);
                        if (postContent == null) continue;

                        oldTagList = Db.TagListParse(postContent.Tags);
                        oldTagListString = Db.TagListJoin(oldTagList);
                        newTagListString = RenameTagAndCreateTagString(oldTagList, details.ListItem.TagName,
                            details.UserNewTagName);

                        if (oldTagListString != newTagListString)
                        {
                            updateVersionAndUpdatedByAndOn(postContent);
                            postContent.Tags = newTagListString;
                        }

                        await Db.SavePostContent(postContent);
                        break;
                    case LinkStream c:
                        var linkStreamContent = db.LinkStreams.SingleOrDefault(x => x.ContentId == c.ContentId);
                        if (linkStreamContent == null) continue;

                        oldTagList = Db.TagListParse(linkStreamContent.Tags);
                        oldTagListString = Db.TagListJoin(oldTagList);
                        newTagListString = RenameTagAndCreateTagString(oldTagList, details.ListItem.TagName,
                            details.UserNewTagName);

                        if (oldTagListString != newTagListString)
                        {
                            updateVersionAndUpdatedByAndOn(linkStreamContent);
                            linkStreamContent.Tags = newTagListString;
                        }

                        await Db.SaveLinkStream(linkStreamContent);
                        break;
                    default:
                        StatusContext.ToastError("Content Type is Unknown?");
                        break;
                }
            }
        }

        public string RenameTagAndCreateTagString(List<string> currentTags, string oldName, string newName)
        {
            return Db.TagListJoin(RenameTagListItem(currentTags, oldName, newName));
        }

        public List<string> RenameTagListItem(List<string> currentTags, string oldName, string newName)
        {
            if (currentTags == null || !currentTags.Any()) return new List<string>();

            if (string.IsNullOrWhiteSpace(oldName) && string.IsNullOrWhiteSpace(newName))
                return Db.TagListCleanup(currentTags);

            if (string.IsNullOrWhiteSpace(oldName)) return AddTagListItem(currentTags, newName);

            if (string.IsNullOrWhiteSpace(newName)) return RemoveTagListItem(currentTags, oldName);

            var removedVersion = RemoveTagListItem(currentTags, oldName);

            return AddTagListItem(removedVersion, newName);
        }

        public async Task ShowDetails(TagListListItem item)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newDetails = new TagListItemDetailDisplay();

            if (item == null)
            {
                StatusContext.ToastWarning("No Item?");
                DetailDisplay = null;
                return;
            }

            newDetails.ListItem = item;

            newDetails.UserNewTagName = item.TagName;
            newDetails.ContentList = new List<TagListItemDetailDisplayContentItem>();


            foreach (var loopContents in item.ContentObjects)
                switch (loopContents)
                {
                    case FileContent c:
                        newDetails.ContentList.Add(
                            new TagListItemDetailDisplayContentItem {Content = c, DisplayText = $"File: {c.Title}"});
                        break;
                    case ImageContent c:
                        newDetails.ContentList.Add(
                            new TagListItemDetailDisplayContentItem {Content = c, DisplayText = $"Image: {c.Title}"});
                        break;
                    case NoteContent c:
                        newDetails.ContentList.Add(
                            new TagListItemDetailDisplayContentItem {Content = c, DisplayText = $"Note: {c.Title}"});
                        break;
                    case PhotoContent c:
                        newDetails.ContentList.Add(
                            new TagListItemDetailDisplayContentItem {Content = c, DisplayText = $"Photo: {c.Title}"});
                        break;
                    case PostContent c:
                        newDetails.ContentList.Add(
                            new TagListItemDetailDisplayContentItem {Content = c, DisplayText = $"Post: {c.Title}"});
                        break;
                    case LinkStream c:
                        newDetails.ContentList.Add(
                            new TagListItemDetailDisplayContentItem {Content = c, DisplayText = $"Link: {c.Title}"});
                        break;
                    default:
                        newDetails.ContentList.Add(new TagListItemDetailDisplayContentItem
                        {
                            Content = null, DisplayText = "Unknown Content Type?"
                        });
                        break;
                }

            DetailDisplay = newDetails;
        }

        public async Task SingleTagContentToExcel(TagListListItem item)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var toTransfer = item.ContentObjects.Select(x => new ContentCommonShell().InjectFrom(x)).ToList();

            ExcelHelpers.ContentToExcelFileAsTable(toTransfer, $"TagDetailFor{item.TagName}");
        }

        public async Task TagsToClipboard()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            if (Items == null || !Items.Any())
            {
                StatusContext.ToastWarning("Nothing to put on Clipboard?");
                return;
            }

            var clipboardText = string.Join(Environment.NewLine, Items.Select(x => x.TagName));

            Clipboard.SetText(clipboardText);
        }
    }
}