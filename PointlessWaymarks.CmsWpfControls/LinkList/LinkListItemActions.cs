using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentHtml.LinkListHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.LinkList
{
    public class LinkListItemActions : IListItemActions<LinkContent>
    {
        private Command<string> _copyUrlCommand;

        private Command<LinkContent> _deleteCommand;
        private Command<LinkContent> _editCommand;
        private Command<LinkContent> _extractNewLinksCommand;
        private Command<LinkContent> _generateHtmlCommand;
        private Command<LinkContent> _linkCodeToClipboardCommand;
        private Command<LinkContent> _openUrlCommand;
        private StatusControlContext _statusContext;
        private Command<LinkContent> _viewHistoryCommand;

        public LinkListItemActions(StatusControlContext statusContext)
        {
            StatusContext = statusContext;
            DeleteCommand = StatusContext.RunBlockingTaskCommand<LinkContent>(Delete);
            EditCommand = StatusContext.RunNonBlockingTaskCommand<LinkContent>(Edit);
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand<LinkContent>(ExtractNewLinks);
            GenerateHtmlCommand = StatusContext.RunBlockingTaskCommand<LinkContent>(GenerateHtml);
            LinkCodeToClipboardCommand = StatusContext.RunBlockingTaskCommand<LinkContent>(LinkCodeToClipboard);
            OpenUrlCommand = StatusContext.RunBlockingTaskCommand<LinkContent>(OpenUrl);
            ViewHistoryCommand = StatusContext.RunNonBlockingTaskCommand<LinkContent>(ViewHistory);

            CopyUrlCommand = StatusContext.RunNonBlockingTaskCommand<string>(async x =>
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                Clipboard.SetText(x);

                StatusContext.ToastSuccess($"To Clipboard {x}");
            });
        }


        public Command<string> CopyUrlCommand
        {
            get => _copyUrlCommand;
            set
            {
                if (Equals(value, _copyUrlCommand)) return;
                _copyUrlCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task Delete(LinkContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (content.Id < 1)
            {
                StatusContext.ToastError($"Link {content.Title} - Entry is not saved - Skipping?");
                return;
            }

            await Db.DeleteLinkContent(content.ContentId, StatusContext.ProgressTracker());
        }

        public Command<LinkContent> DeleteCommand
        {
            get => _deleteCommand;
            set
            {
                if (Equals(value, _deleteCommand)) return;
                _deleteCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task Edit(LinkContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null) return;

            var context = await Db.Context();

            var refreshedData = context.LinkContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null)
                StatusContext.ToastError(
                    $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new LinkContentEditorWindow(refreshedData);

            newContentWindow.PositionWindowAndShow();

            await ThreadSwitcher.ResumeBackgroundAsync();
        }

        public Command<LinkContent> EditCommand
        {
            get => _editCommand;
            set
            {
                if (Equals(value, _editCommand)) return;
                _editCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task ExtractNewLinks(LinkContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var context = await Db.Context();

            var refreshedData = context.LinkContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null) return;

            await LinkExtraction.ExtractNewAndShowLinkContentEditors(
                $"{refreshedData.Comments} {refreshedData.Description}", StatusContext.ProgressTracker());
        }

        public Command<LinkContent> ExtractNewLinksCommand
        {
            get => _extractNewLinksCommand;
            set
            {
                if (Equals(value, _extractNewLinksCommand)) return;
                _extractNewLinksCommand = value;
                OnPropertyChanged();
            }
        }


        public async Task GenerateHtml(LinkContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            StatusContext.Progress("Generating Html for Link List");

            var htmlContext = new LinkListPage();

            htmlContext.WriteLocalHtmlRssAndJson();

            var settings = UserSettingsSingleton.CurrentSettings();

            StatusContext.ToastSuccess($"Generated {settings.LinkListUrl()}");
        }

        public Command<LinkContent> GenerateHtmlCommand
        {
            get => _generateHtmlCommand;
            set
            {
                if (Equals(value, _generateHtmlCommand)) return;
                _generateHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task LinkCodeToClipboard(LinkContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = $"[{content.Title}]({content.Url})";

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        public Command<LinkContent> LinkCodeToClipboardCommand
        {
            get => _linkCodeToClipboardCommand;
            set
            {
                if (Equals(value, _linkCodeToClipboardCommand)) return;
                _linkCodeToClipboardCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task OpenUrl(LinkContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (string.IsNullOrWhiteSpace(content.Url))
            {
                StatusContext.ToastError("URL is Blank?");
                return;
            }

            var url = content.Url;

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        public Command<LinkContent> OpenUrlCommand
        {
            get => _openUrlCommand;
            set
            {
                if (Equals(value, _openUrlCommand)) return;
                _openUrlCommand = value;
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

        public async Task ViewHistory(LinkContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var db = await Db.Context();

            StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

            var historicItems = await db.HistoricLinkContents
                .Where(x => x.ContentId == content.ContentId).ToListAsync();

            StatusContext.Progress($"Found {historicItems.Count} Historic Entries");

            if (historicItems.Count < 1)
            {
                StatusContext.ToastWarning("No History to Show...");
                return;
            }

            var historicView = new ContentViewHistoryPage($"Historic Entries - {content.Title}",
                UserSettingsSingleton.CurrentSettings().SiteName, $"Historic Entries - {content.Title}",
                historicItems.OrderByDescending(x => x.LastUpdatedOn.HasValue).ThenByDescending(x => x.LastUpdatedOn)
                    .Select(ObjectDumper.Dump).ToList());

            historicView.WriteHtmlToTempFolderAndShow(StatusContext.ProgressTracker());
        }

        public Command<LinkContent> ViewHistoryCommand
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

        public static LinkListListItem ListItemFromDbItem(LinkContent content, LinkListItemActions itemActions,
            bool showType)
        {
            return new() {DbEntry = content, ItemActions = itemActions, ShowType = showType};
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}