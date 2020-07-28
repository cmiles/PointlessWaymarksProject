using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsWpfControls.ContentHistoryView;
using PointlessWaymarksCmsWpfControls.LinkContentEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.LinkList
{
    public class LinkListWithActionsContext : INotifyPropertyChanged
    {
        private Command _deleteSelectedCommand;
        private Command _editSelectedContentCommand;
        private Command _generateSelectedHtmlCommand;
        private LinkListContext _listContext;
        private Command _newContentCommand;
        private Command _postCodesToClipboardForSelectedCommand;
        private Command _refreshDataCommand;
        private StatusControlContext _statusContext;

        public LinkListWithActionsContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            EditSelectedContentCommand = new Command(() => StatusContext.RunBlockingTask(EditSelectedContent));
            MdLinkCodesToClipboardForSelectedCommand =
                new Command(() => StatusContext.RunBlockingTask(MdLinkCodesToClipboardForSelected));
            NewContentCommand = new Command(() => StatusContext.RunNonBlockingTask(NewContent));
            RefreshDataCommand = new Command(() => StatusContext.RunBlockingTask(ListContext.LoadData));
            DeleteSelectedCommand = new Command(() => StatusContext.RunBlockingTask(Delete));
            ViewHistoryCommand = new Command(() => StatusContext.RunNonBlockingTask(ViewHistory));

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public Command DeleteSelectedCommand
        {
            get => _deleteSelectedCommand;
            set
            {
                if (Equals(value, _deleteSelectedCommand)) return;
                _deleteSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command EditSelectedContentCommand
        {
            get => _editSelectedContentCommand;
            set
            {
                if (Equals(value, _editSelectedContentCommand)) return;
                _editSelectedContentCommand = value;
                OnPropertyChanged();
            }
        }

        public Command GenerateSelectedHtmlCommand
        {
            get => _generateSelectedHtmlCommand;
            set
            {
                if (Equals(value, _generateSelectedHtmlCommand)) return;
                _generateSelectedHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public LinkListContext ListContext
        {
            get => _listContext;
            set
            {
                if (Equals(value, _listContext)) return;
                _listContext = value;
                OnPropertyChanged();
            }
        }

        public Command MdLinkCodesToClipboardForSelectedCommand
        {
            get => _postCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _postCodesToClipboardForSelectedCommand)) return;
                _postCodesToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command NewContentCommand
        {
            get => _newContentCommand;
            set
            {
                if (Equals(value, _newContentCommand)) return;
                _newContentCommand = value;
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

        public Command ViewHistoryCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task Delete()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var selected = ListContext?.SelectedItems?.OrderBy(x => x.DbEntry.Title).ToList();

            if (selected == null || !selected.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (selected.Count > 1)
                if (await StatusContext.ShowMessage("Delete Multiple Items",
                    $"You are about to delete {selected.Count} items - do you really want to delete all of these items?" +
                    $"{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, selected.Select(x => x.DbEntry.Title))}",
                    new List<string> {"Yes", "No"}) == "No")
                    return;

            var selectedItems = selected.ToList();
            var settings = UserSettingsSingleton.CurrentSettings();

            foreach (var loopSelected in selectedItems)
            {
                if (loopSelected.DbEntry == null || loopSelected.DbEntry.Id < 1)
                {
                    StatusContext.ToastError("Entry is not saved - Skipping?");
                    return;
                }

                await Db.DeleteLinkContent(loopSelected.DbEntry.ContentId, StatusContext.ProgressTracker());
            }
        }

        private async Task EditSelectedContent()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var context = await Db.Context();
            var frozenList = ListContext.SelectedItems;

            foreach (var loopSelected in frozenList)
            {
                var refreshedData =
                    context.LinkContents.SingleOrDefault(x => x.ContentId == loopSelected.DbEntry.ContentId);

                if (refreshedData == null)
                {
                    StatusContext.ToastError(
                        $"{loopSelected.DbEntry.Title} is no longer active in the database? Can not edit - " +
                        "look for a historic version...");
                    continue;
                }

                await ThreadSwitcher.ResumeForegroundAsync();

                var newContentWindow = new LinkContentEditorWindow(refreshedData);

                newContentWindow.Show();

                await ThreadSwitcher.ResumeBackgroundAsync();
            }
        }

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext = new LinkListContext(StatusContext);
        }

        private async Task MdLinkCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = string.Join(", ",
                ListContext.SelectedItems.Select(x => $"[{x.DbEntry.Title}]({x.DbEntry.Url})"));

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task NewContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new LinkContentEditorWindow(null);

            newContentWindow.Show();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task ViewHistory()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var selected = ListContext.SelectedItems;

            if (selected == null || !selected.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (selected.Count > 1)
            {
                StatusContext.ToastError("Please Select a Single Item");
                return;
            }

            var singleSelected = selected.Single();

            if (singleSelected.DbEntry == null || singleSelected.DbEntry.ContentId == Guid.Empty)
            {
                StatusContext.ToastWarning("No History - New/Unsaved Entry?");
                return;
            }

            var db = await Db.Context();

            StatusContext.Progress($"Looking up Historic Entries for {singleSelected.DbEntry.Title}");

            var historicItems = await db.HistoricLinkContents.Where(x => x.ContentId == singleSelected.DbEntry.ContentId)
                .ToListAsync();

            StatusContext.Progress($"Found {historicItems.Count} Historic Entries");

            if (historicItems.Count < 1)
            {
                StatusContext.ToastWarning("No History to Show...");
                return;
            }

            var historicView = new ContentViewHistoryPage($"Historic Entries - {singleSelected.DbEntry.Title}",
                UserSettingsSingleton.CurrentSettings().SiteName, $"Historic Entries - {singleSelected.DbEntry.Title}",
                historicItems.OrderByDescending(x => x.LastUpdatedOn.HasValue).ThenByDescending(x => x.LastUpdatedOn)
                    .Select(ObjectDumper.Dump).ToList());

            historicView.WriteHtmlToTempFolderAndShow(StatusContext.ProgressTracker());
        }
    }
}