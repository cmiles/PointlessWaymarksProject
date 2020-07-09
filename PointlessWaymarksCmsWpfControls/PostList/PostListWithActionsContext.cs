using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using HtmlTags;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.CommonHtml;
using PointlessWaymarksCmsWpfControls.ContentHistoryView;
using PointlessWaymarksCmsWpfControls.PostContentEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;
using SinglePostPage = PointlessWaymarksCmsData.Html.PostHtml.SinglePostPage;

namespace PointlessWaymarksCmsWpfControls.PostList
{
    public class PostListWithActionsContext : INotifyPropertyChanged
    {
        private Command _deleteSelectedCommand;
        private Command _editSelectedContentCommand;
        private Command _emailHtmlToClipboardForSelectedCommand;
        private Command _generateSelectedHtmlCommand;
        private PostListContext _listContext;
        private Command _newContentCommand;
        private Command _openUrlForSelectedCommand;
        private Command _postCodesToClipboardForSelectedCommand;
        private Command _refreshDataCommand;
        private StatusControlContext _statusContext;

        public PostListWithActionsContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            GenerateSelectedHtmlCommand = new Command(() => StatusContext.RunBlockingTask(GenerateSelectedHtml));
            EditSelectedContentCommand = new Command(() => StatusContext.RunBlockingTask(EditSelectedContent));
            PostCodesToClipboardForSelectedCommand =
                new Command(() => StatusContext.RunBlockingTask(PhotoCodesToClipboardForSelected));
            EmailHtmlToClipboardForSelectedCommand = new Command(() => StatusContext.RunBlockingTask(EmailBodyHtml));
            OpenUrlForSelectedCommand = new Command(() => StatusContext.RunNonBlockingTask(OpenUrlForSelected));
            NewContentCommand = new Command(() => StatusContext.RunNonBlockingTask(NewContent));
            RefreshDataCommand = new Command(() => StatusContext.RunBlockingTask(ListContext.LoadData));
            DeleteSelectedCommand = new Command(() => StatusContext.RunBlockingTask(Delete));
            ExtractNewLinksInSelectedCommand =
                new Command(() => StatusContext.RunBlockingTask(ExtractNewLinksInSelected));
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

        public Command EmailHtmlToClipboardForSelectedCommand
        {
            get => _emailHtmlToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _emailHtmlToClipboardForSelectedCommand)) return;
                _emailHtmlToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ExtractNewLinksInSelectedCommand { get; set; }

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

        public PostListContext ListContext
        {
            get => _listContext;
            set
            {
                if (Equals(value, _listContext)) return;
                _listContext = value;
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

        public Command OpenUrlForSelectedCommand
        {
            get => _openUrlForSelectedCommand;
            set
            {
                if (Equals(value, _openUrlForSelectedCommand)) return;
                _openUrlForSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public Command PostCodesToClipboardForSelectedCommand
        {
            get => _postCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _postCodesToClipboardForSelectedCommand)) return;
                _postCodesToClipboardForSelectedCommand = value;
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

            var selected = ListContext.SelectedItems;

            if (selected == null || !selected.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (selected.Count > 1)
            {
                StatusContext.ToastError("Sorry - please delete one at a time");
                return;
            }

            var selectedItem = selected.Single();

            if (selectedItem.DbEntry == null || selectedItem.DbEntry.Id < 1)
            {
                StatusContext.ToastError("Entry is not saved?");
                return;
            }

            var settings = UserSettingsSingleton.CurrentSettings();

            var possibleContentDirectory = settings.LocalSitePostContentDirectory(selectedItem.DbEntry, false);
            if (possibleContentDirectory.Exists) possibleContentDirectory.Delete(true);

            var context = await Db.Context();

            var toHistoric = await context.PostContents.Where(x => x.ContentId == selectedItem.DbEntry.ContentId)
                .ToListAsync();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPostContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                await context.HistoricPostContents.AddAsync(newHistoric);
                context.PostContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            await LoadData();
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
                    context.PostContents.SingleOrDefault(x => x.ContentId == loopSelected.DbEntry.ContentId);

                if (refreshedData == null)
                {
                    StatusContext.ToastError(
                        $"{loopSelected.DbEntry.Title} is no longer active in the database? Can not edit - " +
                        "look for a historic version...");
                    continue;
                }

                await ThreadSwitcher.ResumeForegroundAsync();

                var newContentWindow = new PostContentEditorWindow(refreshedData);

                newContentWindow.Show();

                await ThreadSwitcher.ResumeBackgroundAsync();
            }
        }

        private async Task EmailBodyHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (ListContext.SelectedItems.Count > 1)
            {
                StatusContext.ToastError("Please select only 1 item...");
                return;
            }

            var frozenSelected = ListContext.SelectedItems.First();

            var preprocessResults = BracketCodeCommon.ProcessCodesForEmail(frozenSelected.DbEntry.BodyContent,
                StatusContext.ProgressTracker());
            var bodyHtmlString =
                ContentProcessing.ProcessContent(preprocessResults, frozenSelected.DbEntry.BodyContentFormat);

            var emailCenterTable = new TableTag();
            emailCenterTable.Attr("width", "100%");
            emailCenterTable.Attr("border", "0");
            emailCenterTable.Attr("cellspacing", "0");
            emailCenterTable.Attr("cellpadding", "0");
            var emailCenterRow = emailCenterTable.AddBodyRow();

            var emailCenterLeftCell = emailCenterRow.Cell();
            emailCenterLeftCell.Attr("max-width", "1%");
            emailCenterLeftCell.Attr("align", "center");
            emailCenterLeftCell.Attr("valign", "top");
            emailCenterLeftCell.Text("&nbsp;").Encoded(false);

            var emailCenterContentCell = emailCenterRow.Cell();
            emailCenterContentCell.Attr("width", "100%");
            emailCenterContentCell.Attr("align", "center");
            emailCenterContentCell.Attr("valign", "top");

            var emailCenterRightCell = emailCenterRow.Cell();
            emailCenterRightCell.Attr("max-width", "1%");
            emailCenterRightCell.Attr("align", "center");
            emailCenterRightCell.Attr("valign", "top");
            emailCenterRightCell.Text("&nbsp;").Encoded(false);


            var outerTable = new TableTag();
            emailCenterContentCell.Children.Add(outerTable);
            outerTable.Style("width", "100%");
            outerTable.Style("max-width", "900px");

            var header = new HtmlTag("h3");
            header.Style("text-align", "center");
            var postAddress = $"https:{UserSettingsSingleton.CurrentSettings().PostPageUrl(frozenSelected.DbEntry)}";
            var postLink =
                new LinkTag(
                    $"{UserSettingsSingleton.CurrentSettings().SiteName.HtmlEncode()} - {frozenSelected.DbEntry.Title.HtmlEncode()}",
                    postAddress);
            header.Children.Add(postLink);

            var headerRow = outerTable.AddHeaderRow();
            var headerCell = headerRow.Header();
            headerCell.Children.Add(header);

            var bodyRow = outerTable.AddBodyRow();
            var bodyCell = bodyRow.Cell();
            bodyCell.Text(bodyHtmlString).Encoded(false);

            var footer = new HtmlTag("h4");
            footer.Style("text-align", "center");
            var siteLink = new LinkTag(UserSettingsSingleton.CurrentSettings().SiteUrl,
                @$"https://{UserSettingsSingleton.CurrentSettings().SiteUrl}");
            footer.Children.Add(siteLink);

            var footerRow = outerTable.AddBodyRow();
            var footerCell = footerRow.Cell();
            footerCell.Children.Add(footer);

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(emailCenterTable.ToString());

            StatusContext.ToastSuccess("Post to Email Html on Clipboard");
        }

        private async Task ExtractNewLinksInSelected()
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
                    context.PostContents.SingleOrDefault(x => x.ContentId == loopSelected.DbEntry.ContentId);

                if (refreshedData == null) continue;

                await LinkExtraction.ExtractNewAndShowLinkStreamEditors(
                    $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
            }
        }

        private async Task GenerateSelectedHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var loopCount = 1;
            var totalCount = ListContext.SelectedItems.Count;

            foreach (var loopSelected in ListContext.SelectedItems)
            {
                StatusContext.Progress(
                    $"Generating Html for {loopSelected.DbEntry.Title}, {loopCount} of {totalCount}");

                var htmlContext = new SinglePostPage(loopSelected.DbEntry);

                htmlContext.WriteLocalHtml();

                StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");

                loopCount++;
            }
        }

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext = new PostListContext(StatusContext);
        }

        private async Task NewContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new PostContentEditorWindow(null);

            newContentWindow.Show();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OpenUrlForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var settings = UserSettingsSingleton.CurrentSettings();

            foreach (var loopSelected in ListContext.SelectedItems)
            {
                var url = $@"http://{settings.PostPageUrl(loopSelected.DbEntry)}";

                var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
                Process.Start(ps);
            }
        }

        private async Task PhotoCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = ListContext.SelectedItems.Aggregate(string.Empty,
                (current, loopSelected) =>
                    current + @$"{BracketCodePosts.PostLinkBracketCode(loopSelected.DbEntry)}{Environment.NewLine}");

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
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

            var historicItems = await db.HistoricPostContents
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