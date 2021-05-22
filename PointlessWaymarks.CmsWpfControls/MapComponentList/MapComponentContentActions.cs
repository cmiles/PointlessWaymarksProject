using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.MapComponentData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.MapComponentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapComponentList
{
    public class MapComponentContentActions : IContentActions<MapComponent>
    {
        private Command<MapComponent> _deleteCommand;
        private Command<MapComponent> _editCommand;
        private Command<MapComponent> _extractNewLinksCommand;
        private Command<MapComponent> _generateHtmlCommand;
        private Command<MapComponent> _linkCodeToClipboardCommand;
        private Command<MapComponent> _openUrlCommand;
        private StatusControlContext _statusContext;
        private Command<MapComponent> _viewHistoryCommand;

        public MapComponentContentActions(StatusControlContext statusContext)
        {
            StatusContext = statusContext;
            DeleteCommand = StatusContext.RunBlockingTaskCommand<MapComponent>(Delete);
            EditCommand = StatusContext.RunNonBlockingTaskCommand<MapComponent>(Edit);
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand<MapComponent>(ExtractNewLinks);
            GenerateHtmlCommand = StatusContext.RunBlockingTaskCommand<MapComponent>(GenerateHtml);
            LinkCodeToClipboardCommand =
                StatusContext.RunBlockingTaskCommand<MapComponent>(DefaultBracketCodeToClipboard);
            OpenUrlCommand = StatusContext.RunBlockingTaskCommand<MapComponent>(OpenUrl);
            ViewHistoryCommand = StatusContext.RunNonBlockingTaskCommand<MapComponent>(ViewHistory);
        }

        public string DefaultBracketCode(MapComponent content)
        {
            return content?.ContentId == null ? string.Empty : @$"{BracketCodeMapComponents.Create(content)}";
        }

        public async Task DefaultBracketCodeToClipboard(MapComponent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = @$"{BracketCodeMapComponents.Create(content)}{Environment.NewLine}";

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        public async Task Delete(MapComponent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (content.Id < 1)
            {
                StatusContext.ToastError($"Map {content.Title} - Entry is not saved - Skipping?");
                return;
            }

            await Db.DeleteMapComponent(content.ContentId, StatusContext.ProgressTracker());
        }

        public Command<MapComponent> DeleteCommand
        {
            get => _deleteCommand;
            set
            {
                if (Equals(value, _deleteCommand)) return;
                _deleteCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task Edit(MapComponent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null) return;

            var context = await Db.Context();

            var refreshedData = context.MapComponents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null)
                StatusContext.ToastError(
                    $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new MapComponentEditorWindow(refreshedData);

            newContentWindow.PositionWindowAndShow();

            await ThreadSwitcher.ResumeBackgroundAsync();
        }

        public Command<MapComponent> EditCommand
        {
            get => _editCommand;
            set
            {
                if (Equals(value, _editCommand)) return;
                _editCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task ExtractNewLinks(MapComponent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var context = await Db.Context();

            var refreshedData = context.MapComponents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null) return;

            await LinkExtraction.ExtractNewAndShowLinkContentEditors($"{refreshedData.UpdateNotes}",
                StatusContext.ProgressTracker());
        }

        public Command<MapComponent> ExtractNewLinksCommand
        {
            get => _extractNewLinksCommand;
            set
            {
                if (Equals(value, _extractNewLinksCommand)) return;
                _extractNewLinksCommand = value;
                OnPropertyChanged();
            }
        }


        public async Task GenerateHtml(MapComponent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            StatusContext.Progress($"Generating Html for {content.Title}");

            await MapData.WriteJsonData(content.ContentId);

            StatusContext.ToastSuccess("Generated Map Data");
        }

        public Command<MapComponent> GenerateHtmlCommand
        {
            get => _generateHtmlCommand;
            set
            {
                if (Equals(value, _generateHtmlCommand)) return;
                _generateHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<MapComponent> LinkCodeToClipboardCommand
        {
            get => _linkCodeToClipboardCommand;
            set
            {
                if (Equals(value, _linkCodeToClipboardCommand)) return;
                _linkCodeToClipboardCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task OpenUrl(MapComponent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            StatusContext.ToastWarning("Maps don't have a direct URL to open...");
        }

        public Command<MapComponent> OpenUrlCommand
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

        public async Task ViewHistory(MapComponent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var db = await Db.Context();

            StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

            var historicItems = await db.HistoricMapComponents
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

        public Command<MapComponent> ViewHistoryCommand
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

        public static MapComponentListListItem ListItemFromDbItem(MapComponent content,
            MapComponentContentActions itemActions, bool showType)
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