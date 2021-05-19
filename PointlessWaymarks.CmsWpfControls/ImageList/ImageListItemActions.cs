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
using PointlessWaymarks.CmsData.ContentHtml.ImageHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.ImageContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.ImageList
{
    public class ImageListItemActions : IListItemActions<ImageContent>
    {
        private Command<ImageContent> _deleteCommand;
        private Command<ImageContent> _editCommand;
        private Command<ImageContent> _extractNewLinksCommand;
        private Command<ImageContent> _generateHtmlCommand;
        private Command<ImageContent> _linkCodeToClipboardCommand;
        private Command _newContentCommand;
        private Command<ImageContent> _openUrlCommand;
        private StatusControlContext _statusContext;
        private Command<ImageContent> _viewFileCommand;
        private Command<ImageContent> _viewHistoryCommand;

        public ImageListItemActions(StatusControlContext statusContext)
        {
            StatusContext = statusContext;
            DeleteCommand = StatusContext.RunBlockingTaskCommand<ImageContent>(Delete);
            EditCommand = StatusContext.RunNonBlockingTaskCommand<ImageContent>(Edit);
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand<ImageContent>(ExtractNewLinks);
            GenerateHtmlCommand = StatusContext.RunBlockingTaskCommand<ImageContent>(GenerateHtml);
            LinkCodeToClipboardCommand = StatusContext.RunBlockingTaskCommand<ImageContent>(LinkCodeToClipboard);
            NewContentCommand = StatusContext.RunNonBlockingTaskCommand(NewContent);
            OpenUrlCommand = StatusContext.RunBlockingTaskCommand<ImageContent>(OpenUrl);
            ViewFileCommand = StatusContext.RunNonBlockingTaskCommand<ImageContent>(ViewFile);
            ViewHistoryCommand = StatusContext.RunNonBlockingTaskCommand<ImageContent>(ViewHistory);
        }

        public Command<ImageContent> ViewFileCommand
        {
            get => _viewFileCommand;
            set
            {
                if (Equals(value, _viewFileCommand)) return;
                _viewFileCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task Delete(ImageContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (content.Id < 1)
            {
                StatusContext.ToastError($"Image {content.Title} - Entry is not saved - Skipping?");
                return;
            }

            var settings = UserSettingsSingleton.CurrentSettings();

            await Db.DeleteImageContent(content.ContentId, StatusContext.ProgressTracker());

            var possibleContentDirectory = settings.LocalSiteImageContentDirectory(content, false);
            if (possibleContentDirectory.Exists)
            {
                StatusContext.Progress($"Deleting Generated Folder {possibleContentDirectory.FullName}");
                possibleContentDirectory.Delete(true);
            }
        }

        public Command<ImageContent> DeleteCommand
        {
            get => _deleteCommand;
            set
            {
                if (Equals(value, _deleteCommand)) return;
                _deleteCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task Edit(ImageContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null) return;

            var context = await Db.Context();

            var refreshedData = context.ImageContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null)
                StatusContext.ToastError(
                    $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new ImageContentEditorWindow(refreshedData);

            newContentWindow.PositionWindowAndShow();

            await ThreadSwitcher.ResumeBackgroundAsync();
        }

        public Command<ImageContent> EditCommand
        {
            get => _editCommand;
            set
            {
                if (Equals(value, _editCommand)) return;
                _editCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task ExtractNewLinks(ImageContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var context = await Db.Context();
            var refreshedData = context.ImageContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null) return;

            await LinkExtraction.ExtractNewAndShowLinkContentEditors(
                $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
        }

        public Command<ImageContent> ExtractNewLinksCommand
        {
            get => _extractNewLinksCommand;
            set
            {
                if (Equals(value, _extractNewLinksCommand)) return;
                _extractNewLinksCommand = value;
                OnPropertyChanged();
            }
        }


        public async Task GenerateHtml(ImageContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            StatusContext.Progress($"Generating Html for {content.Title}");

            var htmlContext = new SingleImagePage(content);

            htmlContext.WriteLocalHtml();

            StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");
        }

        public Command<ImageContent> GenerateHtmlCommand
        {
            get => _generateHtmlCommand;
            set
            {
                if (Equals(value, _generateHtmlCommand)) return;
                _generateHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task LinkCodeToClipboard(ImageContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = @$"{BracketCodeImages.Create(content)}{Environment.NewLine}";

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        public Command<ImageContent> LinkCodeToClipboardCommand
        {
            get => _linkCodeToClipboardCommand;
            set
            {
                if (Equals(value, _linkCodeToClipboardCommand)) return;
                _linkCodeToClipboardCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task NewContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new ImageContentEditorWindow();

            newContentWindow.PositionWindowAndShow();
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

        public async Task OpenUrl(ImageContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var settings = UserSettingsSingleton.CurrentSettings();

            var url = $@"http://{settings.ImagePageUrl(content)}";

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        public Command<ImageContent> OpenUrlCommand
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

        public async Task ViewHistory(ImageContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var db = await Db.Context();

            StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

            var historicItems = await db.HistoricImageContents
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

        public Command<ImageContent> ViewHistoryCommand
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

        public static ImageListListItem ListItemFromDbItem(ImageContent content, ImageListItemActions itemActions,
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


        public async Task ViewFile(ImageContent listItem)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (listItem == null)
            {
                StatusContext.ToastError("Nothing Items to Open?");
                return;
            }

            if (string.IsNullOrWhiteSpace(listItem.OriginalFileName))
            {
                StatusContext.ToastError("No Image?");
                return;
            }

            var toOpen = UserSettingsSingleton.CurrentSettings().LocalSiteImageContentFile(listItem);

            if (toOpen is not {Exists: true})
            {
                StatusContext.ToastError("Image doesn't exist?");
                return;
            }

            var url = toOpen.FullName;

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }
    }
}