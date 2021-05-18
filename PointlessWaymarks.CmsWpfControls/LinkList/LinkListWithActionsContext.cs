using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HtmlTableHelper;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using pinboard.net;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.HtmlViewer;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.LinkList
{
    public class LinkListWithActionsContext : INotifyPropertyChanged
    {
        private Command _deleteSelectedCommand;
        private Command _editSelectedContentCommand;
        private Command _importFromExcelFileCommand;
        private Command _importFromOpenExcelInstanceCommand;
        private ContentListContext _listContext;
        private Command _listSelectedLinksNotOnPinboardCommand;
        private Command _newContentCommand;
        private Command _postCodesToClipboardForSelectedCommand;
        private Command _refreshDataCommand;
        private Command _selectedToExcelCommand;
        private StatusControlContext _statusContext;
        private Command _viewHistoryCommand;

        public LinkListWithActionsContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

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

        public ContentListContext ListContext
        {
            get => _listContext;
            set
            {
                if (Equals(value, _listContext)) return;
                _listContext = value;
                OnPropertyChanged();
            }
        }

        public Command ListSelectedLinksNotOnPinboardCommand
        {
            get => _listSelectedLinksNotOnPinboardCommand;
            set
            {
                if (Equals(value, _listSelectedLinksNotOnPinboardCommand)) return;
                _listSelectedLinksNotOnPinboardCommand = value;
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

        public Command SelectedToExcelCommand
        {
            get => _selectedToExcelCommand;
            set
            {
                if (Equals(value, _selectedToExcelCommand)) return;
                _selectedToExcelCommand = value;
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

        public Command ViewHistoryCommand
        {
            get => _viewHistoryCommand;
            set
            {
                if (Equals(value, _viewHistoryCommand)) return;
                _viewHistoryCommand = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task CopySelectedItemUrlsToClipboard()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var frozenSelected = SelectedItems();

            if (!frozenSelected.Any())
            {
                StatusContext.ToastWarning("Nothing selected?");
                return;
            }

            var clipboardText = string.Join(Environment.NewLine, frozenSelected.Select(x => x.DbEntry.Url));

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(clipboardText);
        }

        private async Task Delete()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var selected = SelectedItems().OrderBy(x => x.DbEntry.Title).ToList();

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

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var context = await Db.Context();
            var frozenList = SelectedItems();

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

                newContentWindow.PositionWindowAndShow();

                await ThreadSwitcher.ResumeBackgroundAsync();
            }
        }

        private void ExecuteListBoxItemsCopy(object sender, ExecutedRoutedEventArgs e)
        {
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(CopySelectedItemUrlsToClipboard);
        }


        private async Task ListSelectedLinksNotOnPinboard(IProgress<string> progress)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().PinboardApiToken))
            {
                progress?.Report("No Pinboard Api Token... Can't check Pinboard.");
                return;
            }

            var selected = SelectedItems().ToList();

            if (!selected.Any())
            {
                progress?.Report("Nothing Selected?");
                return;
            }

            progress?.Report($"Found {selected.Count} items to check.");


            using var pb = new PinboardAPI(UserSettingsSingleton.CurrentSettings().PinboardApiToken);

            var notFoundList = new List<LinkContent>();

            foreach (var loopSelected in selected)
            {
                if (string.IsNullOrWhiteSpace(loopSelected.DbEntry.Url))
                {
                    notFoundList.Add(loopSelected.DbEntry);
                    progress?.Report(
                        $"Link titled {loopSelected.DbEntry.Title} created on {loopSelected.DbEntry.CreatedOn:d} added because of blank URL...");
                    continue;
                }

                var matches = await pb.Posts.Get(null, null, loopSelected.DbEntry.Url);

                if (!matches.Posts.Any())
                {
                    progress?.Report(
                        $"Not Found Link titled {loopSelected.DbEntry.Title} created on {loopSelected.DbEntry.CreatedOn:d}");
                    notFoundList.Add(loopSelected.DbEntry);
                }
                else
                {
                    progress?.Report(
                        $"Found Link titled {loopSelected.DbEntry.Title} created on {loopSelected.DbEntry.CreatedOn:d}");
                }
            }

            if (!notFoundList.Any())
            {
                await StatusContext.ShowMessageWithOkButton("Pinboard Match Complete",
                    $"Found a match on Pinboard for all {selected.Count} Selected links.");
                return;
            }

            progress?.Report($"Building table of {notFoundList.Count} items not found on Pinboard");

            var projectedNotFound = notFoundList.Select(x => new
            {
                x.Title,
                x.Url,
                x.CreatedBy,
                x.CreatedOn,
                x.LastUpdatedBy,
                x.LastUpdatedOn
            }).ToHtmlTable(new {@class = "pure-table pure-table-striped"});

            await ThreadSwitcher.ResumeForegroundAsync();

            var htmlReportWindow =
                new HtmlViewerWindow(
                    projectedNotFound.ToHtmlDocumentWithPureCss("Links Not In Pinboard", string.Empty));
            htmlReportWindow.PositionWindowAndShow();
        }

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext = new ContentListContext(StatusContext, new LinkListLoader(100));

            RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);
            EditSelectedContentCommand = StatusContext.RunBlockingTaskCommand(EditSelectedContent);
            MdLinkCodesToClipboardForSelectedCommand =
                StatusContext.RunBlockingTaskCommand(MdLinkCodesToClipboardForSelected);
            NewContentCommand = StatusContext.RunNonBlockingTaskCommand(NewContent);
            DeleteSelectedCommand = StatusContext.RunBlockingTaskCommand(Delete);
            ViewHistoryCommand = StatusContext.RunNonBlockingTaskCommand(ViewHistory);

            ImportFromExcelFileCommand =
                StatusContext.RunBlockingTaskCommand(async () => await ExcelHelpers.ImportFromExcelFile(StatusContext));
            ImportFromOpenExcelInstanceCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await ExcelHelpers.ImportFromOpenExcelInstance(StatusContext));
            SelectedToExcelCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
                await ExcelHelpers.SelectedToExcel(SelectedItems()?.Cast<dynamic>().ToList(), StatusContext));
            ListSelectedLinksNotOnPinboardCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await ListSelectedLinksNotOnPinboard(StatusContext.ProgressTracker()));

            await ListContext.LoadData();
        }

        private async Task MdLinkCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItems() == null || !SelectedItems().Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = string.Join(", ",
                SelectedItems().Select(x => $"[{x.DbEntry.Title}]({x.DbEntry.Url})"));

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task NewContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new LinkContentEditorWindow(null);

            newContentWindow.PositionWindowAndShow();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<LinkListListItem> SelectedItems()
        {
            return ListContext?.ListSelection?.SelectedItems?.Where(x => x is LinkListListItem)
                .Cast<LinkListListItem>()
                .ToList() ?? new List<LinkListListItem>();
        }

        private async Task ViewHistory()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var selected = SelectedItems();

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

            var historicItems = await db.HistoricLinkContents
                .Where(x => x.ContentId == singleSelected.DbEntry.ContentId).ToListAsync();

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