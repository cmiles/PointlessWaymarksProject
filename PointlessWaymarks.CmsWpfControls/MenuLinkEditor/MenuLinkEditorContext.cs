﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.MenuLinkEditor
{
    public class MenuLinkEditorContext : INotifyPropertyChanged
    {
        private Command _addItemCommand;
        private string _helpMarkdown;
        private ObservableCollection<MenuLinkListItem> _items = new();
        private List<MenuLinkListItem> _selectedItems;
        private StatusControlContext _statusContext;

        public MenuLinkEditorContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            AddItemCommand = StatusContext.RunBlockingTaskCommand(AddItem);
            DeleteItemCommand = StatusContext.RunBlockingTaskCommand(DeleteItems);
            MoveItemUpCommand = StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(MoveItemUp);
            MoveItemDownCommand = StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(MoveItemDown);
            SaveCommand = StatusContext.RunBlockingTaskCommand(Save);
            InsertIndexTagIndexCommand =
                StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(x =>
                    InsertIntoLinkTag(x, "{{index; text Main;}}"));
            InsertTagSearchCommand =
                StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(x =>
                    InsertIntoLinkTag(x, "{{tagspage; text Tags;}}"));
            InsertPhotoGalleryCommand =
                StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(x =>
                    InsertIntoLinkTag(x, "{{photogallerypage; text Photos;}}"));
            InsertSearchPageCommand =
                StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(x =>
                    InsertIntoLinkTag(x, "{{searchpage; text Search;}}"));
            InsertLinkListCommand =
                StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(x =>
                    InsertIntoLinkTag(x, "{{linklistpage; text Links;}}"));

            HelpMarkdown = MenuLinksHelpMarkdown.HelpBlock;

            StatusContext.RunFireAndForgetBlockingTask(LoadData);
        }

        public Command AddItemCommand
        {
            get => _addItemCommand;
            set
            {
                if (Equals(value, _addItemCommand)) return;
                _addItemCommand = value;
                OnPropertyChanged();
            }
        }

        public Command DeleteItemCommand { get; set; }

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

        public Command<MenuLinkListItem> InsertIndexTagIndexCommand { get; set; }

        public Command<MenuLinkListItem> InsertLinkListCommand { get; set; }

        public Command<MenuLinkListItem> InsertPhotoGalleryCommand { get; set; }

        public Command<MenuLinkListItem> InsertSearchPageCommand { get; set; }

        public Command<MenuLinkListItem> InsertTagSearchCommand { get; set; }

        public ObservableCollection<MenuLinkListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public Command<MenuLinkListItem> MoveItemDownCommand { get; set; }

        public Command MoveItemUpCommand { get; set; }

        public Command SaveCommand { get; set; }

        public List<MenuLinkListItem> SelectedItems
        {
            get => _selectedItems;
            set
            {
                if (Equals(value, _selectedItems)) return;
                _selectedItems = value;
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

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task AddItem()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newItem = new MenuLinkListItem {DbEntry = new MenuLink(), UserOrder = Items.Count};

            await ThreadSwitcher.ResumeForegroundAsync();

            Items.Add(newItem);
        }

        private async Task DeleteItems()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var selected = SelectedItems;

            if (selected == null || !selected.Any())
            {
                StatusContext.ToastError("Nothing Selected to Delete?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            selected.ForEach(x => Items.Remove(x));

            await RenumberItems();
        }

        private async Task InsertIntoLinkTag(MenuLinkListItem listItem, string toInsert)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (listItem == null)
            {
                StatusContext.ToastError("No item?");
                return;
            }

            listItem.UserLink = (listItem.UserLink ?? string.Empty).Trim();

            listItem.UserLink += toInsert;
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var context = await Db.Context();
            var existingEntries = await context.MenuLinks.ToListAsync();
            var listItems = existingEntries.Select(x =>
                    new MenuLinkListItem {DbEntry = x, UserLink = x.LinkTag?.Trim() ?? string.Empty})
                .OrderBy(x => x.UserOrder).ThenBy(x => x.UserLink).ToList();

            await ThreadSwitcher.ResumeForegroundAsync();

            Items.Clear();

            listItems.ForEach(x => Items.Add(x));

            await RenumberItems();
        }

        private async Task MoveItemDown(MenuLinkListItem listItem)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            await RenumberItems();

            var currentItemIndex = Items.IndexOf(listItem);

            if (currentItemIndex == Items.Count - 1) return;

            await ThreadSwitcher.ResumeForegroundAsync();

            Items.Move(currentItemIndex, currentItemIndex + 1);

            await RenumberItems();
        }

        private async Task MoveItemUp(MenuLinkListItem listItem)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            await RenumberItems();

            var currentItemIndex = Items.IndexOf(listItem);

            if (currentItemIndex == 0) return;

            await ThreadSwitcher.ResumeForegroundAsync();

            Items.Move(currentItemIndex, currentItemIndex - 1);

            await RenumberItems();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task RenumberItems()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            for (var i = 0; i < Items.Count; i++) Items[i].UserOrder = i;
        }

        private async Task Save()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (Items == null || !Items.Any())
            {
                StatusContext.ToastError("No entries to save?");
                return;
            }

            foreach (var loopItems in Items) loopItems.UserLink = (loopItems.UserLink ?? string.Empty).Trim();

            await RenumberItems();

            var withChanges = Items.Where(x => x.HasChanges).ToList();

            if (!withChanges.Any())
            {
                StatusContext.ToastError("No entries have changed?");
                return;
            }

            if (withChanges.Any(x => string.IsNullOrWhiteSpace(x.UserLink)))
            {
                StatusContext.ToastError("All Entries must have a value.");
                return;
            }

            var context = await Db.Context();

            var frozenNowVersion = DateTime.Now.ToUniversalTime().TrimDateTimeToSeconds();

            foreach (var loopChanges in withChanges)
                if (loopChanges.DbEntry == null || loopChanges.DbEntry.Id < 1)
                {
                    loopChanges.DbEntry = new MenuLink
                    {
                        LinkTag = loopChanges.UserLink,
                        MenuOrder = loopChanges.UserOrder,
                        ContentVersion = frozenNowVersion
                    };

                    await context.MenuLinks.AddAsync(loopChanges.DbEntry);
                }
                else
                {
                    var toUpdate = await context.MenuLinks.SingleOrDefaultAsync(x => x.Id == loopChanges.DbEntry.Id);

                    toUpdate.LinkTag = loopChanges.UserLink;
                    toUpdate.MenuOrder = loopChanges.UserOrder;
                    toUpdate.ContentVersion = frozenNowVersion;

                    loopChanges.DbEntry = toUpdate;
                }

            await context.SaveChangesAsync(true);

            StatusContext.ToastSuccess("Saved Changes");
        }
    }
}