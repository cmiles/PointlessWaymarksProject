using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.PointList
{
    public class PointContentActions : IContentActions<PointContent>
    {
        private Command<PointContent> _deleteCommand;
        private Command<PointContent> _editCommand;
        private Command<PointContent> _extractNewLinksCommand;
        private Command<PointContent> _generateHtmlCommand;
        private Command<PointContent> _linkCodeToClipboardCommand;
        private Command<PointContent> _openUrlCommand;
        private StatusControlContext _statusContext;
        private Command<PointContent> _viewHistoryCommand;

        public PointContentActions(StatusControlContext statusContext)
        {
            StatusContext = statusContext;
            DeleteCommand = StatusContext.RunBlockingTaskCommand<PointContent>(Delete);
            EditCommand = StatusContext.RunNonBlockingTaskCommand<PointContent>(Edit);
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand<PointContent>(ExtractNewLinks);
            GenerateHtmlCommand = StatusContext.RunBlockingTaskCommand<PointContent>(GenerateHtml);
            LinkCodeToClipboardCommand =
                StatusContext.RunBlockingTaskCommand<PointContent>(DefaultBracketCodeToClipboard);
            OpenUrlCommand = StatusContext.RunBlockingTaskCommand<PointContent>(OpenUrl);
            ViewHistoryCommand = StatusContext.RunNonBlockingTaskCommand<PointContent>(ViewHistory);
        }

        public string DefaultBracketCode(PointContent content)
        {
            if (content?.ContentId == null) return string.Empty;
            return @$"{BracketCodePoints.Create(content)}";
        }

        public async Task DefaultBracketCodeToClipboard(PointContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = @$"{BracketCodePoints.Create(content)}{Environment.NewLine}";

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        public async Task Delete(PointContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (content.Id < 1)
            {
                StatusContext.ToastError($"Point {content.Title} - Entry is not saved - Skipping?");
                return;
            }

            var settings = UserSettingsSingleton.CurrentSettings();

            await Db.DeletePointContent(content.ContentId, StatusContext.ProgressTracker());

            var possibleContentDirectory = settings.LocalSitePointContentDirectory(content, false);
            if (possibleContentDirectory.Exists)
            {
                StatusContext.Progress($"Deleting Generated Folder {possibleContentDirectory.FullName}");
                possibleContentDirectory.Delete(true);
            }
        }

        public Command<PointContent> DeleteCommand
        {
            get => _deleteCommand;
            set
            {
                if (Equals(value, _deleteCommand)) return;
                _deleteCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task Edit(PointContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null) return;

            var context = await Db.Context();

            var refreshedData = context.PointContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null)
                StatusContext.ToastError(
                    $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new PointContentEditorWindow(refreshedData);

            newContentWindow.PositionWindowAndShow();

            await ThreadSwitcher.ResumeBackgroundAsync();
        }

        public Command<PointContent> EditCommand
        {
            get => _editCommand;
            set
            {
                if (Equals(value, _editCommand)) return;
                _editCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task ExtractNewLinks(PointContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var context = await Db.Context();

            var refreshedData = context.PointContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null) return;

            await LinkExtraction.ExtractNewAndShowLinkContentEditors(
                $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
        }

        public Command<PointContent> ExtractNewLinksCommand
        {
            get => _extractNewLinksCommand;
            set
            {
                if (Equals(value, _extractNewLinksCommand)) return;
                _extractNewLinksCommand = value;
                OnPropertyChanged();
            }
        }


        public async Task GenerateHtml(PointContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            StatusContext.Progress($"Generating Html for {content.Title}");

            var fullItem = await Db.PointAndPointDetails(content.ContentId);

            if (fullItem == null)
            {
                StatusContext.ToastError("Item no longer exists in DB?");
                return;
            }

            var htmlContext = new SinglePointPage(fullItem);

            await htmlContext.WriteLocalHtml();

            StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");
        }

        public Command<PointContent> GenerateHtmlCommand
        {
            get => _generateHtmlCommand;
            set
            {
                if (Equals(value, _generateHtmlCommand)) return;
                _generateHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<PointContent> LinkCodeToClipboardCommand
        {
            get => _linkCodeToClipboardCommand;
            set
            {
                if (Equals(value, _linkCodeToClipboardCommand)) return;
                _linkCodeToClipboardCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task OpenUrl(PointContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var settings = UserSettingsSingleton.CurrentSettings();

            var url = $@"http://{settings.PointPageUrl(content)}";

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        public Command<PointContent> OpenUrlCommand
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

        public async Task ViewHistory(PointContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var db = await Db.Context();

            StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

            var historicItems = await db.HistoricPointContents
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

        public Command<PointContent> ViewHistoryCommand
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

        public static PointListListItem ListItemFromDbItem(PointContent content, PointContentActions itemActions,
            bool showType)
        {
            return new()
            {
                DbEntry = content,
                SmallImageUrl = ContentListContext.GetSmallImageUrl(content),
                ItemActions = itemActions,
                ShowType = showType
            };
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}