using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.HelpDisplay;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.TagExclusionEditor
{
    public class TagExclusionEditorContext : INotifyPropertyChanged
    {
        private string _helpMarkdown;
        private ObservableRangeCollection<TagExclusionEditorListItem> _items;
        private StatusControlContext _statusContext;

        public TagExclusionEditorContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            HelpMarkdown = TagExclusionHelpMarkdown.HelpBlock;
            AddNewItemCommand = new Command(async () => await AddNewItem());
            SaveItemCommand = new Command<TagExclusionEditorListItem>(x => StatusContext.RunBlockingTask(SaveItem, x));
            DeleteItemCommand =
                new Command<TagExclusionEditorListItem>(x => StatusContext.RunBlockingTask(DeleteItem, x));

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public Command AddNewItemCommand { get; set; }

        public Command<TagExclusionEditorListItem> DeleteItemCommand { get; set; }

        public string HelpMarkdown
        {
            get => _helpMarkdown;
            set
            {
                if (value == _helpMarkdown) return;
                _helpMarkdown = value;
                OnPropertyChanged();
            }
        }

        public ObservableRangeCollection<TagExclusionEditorListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public Command<TagExclusionEditorListItem> SaveItemCommand { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task AddNewItem()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            Items.Add(new TagExclusionEditorListItem {DbEntry = new TagExclusion()});
        }

        private async Task DeleteItem(TagExclusionEditorListItem tagItem)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (tagItem.DbEntry == null || tagItem.DbEntry.Id < 1)
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                Items.Remove(tagItem);
                return;
            }

            var db = await Db.Context();

            db.TagExclusions.Remove(tagItem.DbEntry);

            await ThreadSwitcher.ResumeForegroundAsync();

            Items.Remove(tagItem);
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            var dbItems = await db.TagExclusions.ToListAsync();

            var listItems = dbItems.Select(x => new TagExclusionEditorListItem {DbEntry = x, TagValue = x.Tag})
                .OrderBy(x => x.TagValue).ToList();

            if (Items == null)
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                Items = new ObservableRangeCollection<TagExclusionEditorListItem>();
                await ThreadSwitcher.ResumeBackgroundAsync();
            }

            foreach (var loopListItem in listItems)
            {
                var possibleItem = Items.SingleOrDefault(x => x.DbEntry?.Id == loopListItem.DbEntry.Id);

                if (possibleItem == null)
                {
                    await ThreadSwitcher.ResumeForegroundAsync();
                    Items.Add(loopListItem);
                    await ThreadSwitcher.ResumeBackgroundAsync();
                }
                else
                {
                    possibleItem.DbEntry = loopListItem.DbEntry;
                }
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task SaveItem(TagExclusionEditorListItem tagItem)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            tagItem.TagValue = tagItem.TagValue.TrimNullSafe();

            if (string.IsNullOrWhiteSpace(tagItem.TagValue))
            {
                StatusContext.ToastError("Blank tags can not be excluded.");
                return;
            }

            var db = await Db.Context();

            tagItem.DbEntry ??= new TagExclusion();

            if (tagItem.DbEntry.Id < 1)
            {
                var toAdd = new TagExclusion {Tag = tagItem.TagValue};
                await db.AddAsync(toAdd);
                await db.SaveChangesAsync(true);
                tagItem.DbEntry = toAdd;
                return;
            }

            var toModify = await db.TagExclusions.SingleAsync(x => x.Id == tagItem.DbEntry.Id);

            toModify.Tag = tagItem.TagValue;

            await db.SaveChangesAsync(true);

            tagItem.DbEntry = toModify;
        }
    }
}