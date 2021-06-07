using System;
using System.Collections.Generic;
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
using PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.PhotoList
{
    public class PhotoContentActions : IContentActions<PhotoContent>
    {
        private Command<PhotoContent> _apertureSearchCommand;
        private Command<PhotoContent> _cameraMakeSearchCommand;
        private Command<PhotoContent> _cameraModelSearchCommand;
        private Command<PhotoContent> _deleteCommand;
        private Command<PhotoContent> _editCommand;
        private Command<PhotoContent> _extractNewLinksCommand;
        private Command<PhotoContent> _focalLengthSearchCommand;
        private Command<PhotoContent> _generateHtmlCommand;
        private Command<PhotoContent> _isoSearchCommand;
        private Command<PhotoContent> _lensSearchCommand;
        private Command<PhotoContent> _linkCodeToClipboardCommand;
        private Command<PhotoContent> _openUrlCommand;
        private Command<PhotoContent> _photoTakenOnSearchCommand;
        private Command<PhotoContent> _shutterSpeedSearchCommand;
        private StatusControlContext _statusContext;
        private Command<PhotoContent> _viewFileCommand;

        public PhotoContentActions(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            DeleteCommand = StatusContext.RunBlockingTaskCommand<PhotoContent>(Delete);
            EditCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(Edit);
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand<PhotoContent>(ExtractNewLinks);
            GenerateHtmlCommand = StatusContext.RunBlockingTaskCommand<PhotoContent>(GenerateHtml);
            LinkCodeToClipboardCommand =
                StatusContext.RunBlockingTaskCommand<PhotoContent>(DefaultBracketCodeToClipboard);
            OpenUrlCommand = StatusContext.RunBlockingTaskCommand<PhotoContent>(OpenUrl);
            ViewFileCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(ViewFile);
            ViewHistoryCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(ViewHistory);

            ApertureSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await ApertureSearch(x), $"Aperture - {x.Aperture}"));
            CameraMakeSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await CameraMakeSearch(x), $"Camera Make - {x.CameraMake}"));
            CameraModelSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await CameraModelSearch(x), $"Camera Model - {x.CameraModel}"));
            FocalLengthSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await FocalLengthSearch(x), $"Focal Length - {x.FocalLength}"));
            IsoSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await IsoSearch(x), $"ISO - {x.Iso}"));
            LensSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await LensSearch(x), $"Lens - {x.Lens}"));
            PhotoTakenOnSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await PhotoTakenOnSearch(x),
                    $"Photo Created On - {x.PhotoCreatedOn.Date:D}"));
            ShutterSpeedSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await ShutterSpeedSearch(x), $"Shutter Speed - {x.ShutterSpeed}"));
        }


        public Command<PhotoContent> ApertureSearchCommand
        {
            get => _apertureSearchCommand;
            set
            {
                if (Equals(value, _apertureSearchCommand)) return;
                _apertureSearchCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<PhotoContent> CameraMakeSearchCommand
        {
            get => _cameraMakeSearchCommand;
            set
            {
                if (Equals(value, _cameraMakeSearchCommand)) return;
                _cameraMakeSearchCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<PhotoContent> CameraModelSearchCommand
        {
            get => _cameraModelSearchCommand;
            set
            {
                if (Equals(value, _cameraModelSearchCommand)) return;
                _cameraModelSearchCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<PhotoContent> FocalLengthSearchCommand
        {
            get => _focalLengthSearchCommand;
            set
            {
                if (Equals(value, _focalLengthSearchCommand)) return;
                _focalLengthSearchCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<PhotoContent> IsoSearchCommand
        {
            get => _isoSearchCommand;
            set
            {
                if (Equals(value, _isoSearchCommand)) return;
                _isoSearchCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<PhotoContent> LensSearchCommand
        {
            get => _lensSearchCommand;
            set
            {
                if (Equals(value, _lensSearchCommand)) return;
                _lensSearchCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<PhotoContent> PhotoTakenOnSearchCommand
        {
            get => _photoTakenOnSearchCommand;
            set
            {
                if (Equals(value, _photoTakenOnSearchCommand)) return;
                _photoTakenOnSearchCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<PhotoContent> ShutterSpeedSearchCommand
        {
            get => _shutterSpeedSearchCommand;
            set
            {
                if (Equals(value, _shutterSpeedSearchCommand)) return;
                _shutterSpeedSearchCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<PhotoContent> ViewFileCommand
        {
            get => _viewFileCommand;
            set
            {
                if (Equals(value, _viewFileCommand)) return;
                _viewFileCommand = value;
                OnPropertyChanged();
            }
        }

        public string DefaultBracketCode(PhotoContent content)
        {
            return content?.ContentId == null ? string.Empty : @$"{BracketCodePhotos.Create(content)}";
        }

        public async Task DefaultBracketCodeToClipboard(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = @$"{BracketCodePhotos.Create(content)}{Environment.NewLine}";

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        public async Task Delete(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            if (content.Id < 1)
            {
                StatusContext.ToastError("Entry is not saved - Skipping?");
                return;
            }

            var settings = UserSettingsSingleton.CurrentSettings();

            await Db.DeletePhotoContent(content.ContentId, StatusContext.ProgressTracker());

            var possibleContentDirectory = settings.LocalSitePhotoContentDirectory(content, false);

            if (possibleContentDirectory.Exists)
            {
                StatusContext.Progress($"Deleting Generated Folder {possibleContentDirectory.FullName}");
                possibleContentDirectory.Delete(true);
            }
        }

        public Command<PhotoContent> DeleteCommand
        {
            get => _deleteCommand;
            set
            {
                if (Equals(value, _deleteCommand)) return;
                _deleteCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task Edit(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null) return;

            var context = await Db.Context();

            var refreshedData = context.PhotoContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null)
                StatusContext.ToastError(
                    $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new PhotoContentEditorWindow(refreshedData);

            newContentWindow.PositionWindowAndShow();

            await ThreadSwitcher.ResumeBackgroundAsync();
        }

        public Command<PhotoContent> EditCommand
        {
            get => _editCommand;
            set
            {
                if (Equals(value, _editCommand)) return;
                _editCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task ExtractNewLinks(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var context = await Db.Context();
            var refreshedData = context.PhotoContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null) return;

            await LinkExtraction.ExtractNewAndShowLinkContentEditors(
                $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
        }

        public Command<PhotoContent> ExtractNewLinksCommand
        {
            get => _extractNewLinksCommand;
            set
            {
                if (Equals(value, _extractNewLinksCommand)) return;
                _extractNewLinksCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task GenerateHtml(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            StatusContext.Progress($"Generating Html for {content.Title}");

            var htmlContext = new SinglePhotoPage(content);

            await htmlContext.WriteLocalHtml();

            StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");
        }

        public Command<PhotoContent> GenerateHtmlCommand
        {
            get => _generateHtmlCommand;
            set
            {
                if (Equals(value, _generateHtmlCommand)) return;
                _generateHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<PhotoContent> LinkCodeToClipboardCommand
        {
            get => _linkCodeToClipboardCommand;
            set
            {
                if (Equals(value, _linkCodeToClipboardCommand)) return;
                _linkCodeToClipboardCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task OpenUrl(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var settings = UserSettingsSingleton.CurrentSettings();

            var url = $@"http://{settings.PhotoPageUrl(content)}";

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        public Command<PhotoContent> OpenUrlCommand
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

        public async Task ViewHistory(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null)
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var db = await Db.Context();

            StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

            var historicItems = await db.HistoricPhotoContents
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

        public Command<PhotoContent> ViewHistoryCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private static async Task<List<object>> ApertureSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.Aperture == content.Aperture).ToListAsync()).Cast<object>()
                .ToList();
        }

        public static async Task<List<object>> CameraMakeSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.CameraMake == content.CameraMake).ToListAsync()).Cast<object>()
                .ToList();
        }

        public static async Task<List<object>> CameraModelSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.CameraModel == content.CameraModel).ToListAsync())
                .Cast<object>().ToList();
        }

        public async Task EditContent(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null) return;

            var context = await Db.Context();

            var refreshedData = context.PhotoContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null)
                StatusContext.ToastError($"{content.Title} is no longer active in the database? Can not edit - " +
                                         "look for a historic version...");

            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new PhotoContentEditorWindow(refreshedData);

            newContentWindow.PositionWindowAndShow();

            await ThreadSwitcher.ResumeBackgroundAsync();
        }


        public static async Task<List<object>> FocalLengthSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.FocalLength == content.FocalLength).ToListAsync())
                .Cast<object>().ToList();
        }

        public static async Task<List<object>> IsoSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.Iso == content.Iso).ToListAsync()).Cast<object>().ToList();
        }

        public static async Task<List<object>> LensSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.Lens == content.Lens).ToListAsync()).Cast<object>().ToList();
        }

        public static PhotoListListItem ListItemFromDbItem(PhotoContent content,
            PhotoContentActions photoContentActions, bool showType)
        {
            return new()
            {
                DbEntry = content,
                SmallImageUrl = ContentListContext.GetSmallImageUrl(content),
                ItemActions = photoContentActions,
                ShowType = showType
            };
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static async Task<List<object>> PhotoTakenOnSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            //Todo: I think this should be possible via something like DbFunctions or EF functions?
            //I didn't understand what approach to take from a few google searches...
            var dateTimeAfter = content.PhotoCreatedOn.Date;
            var dateTimeBefore = content.PhotoCreatedOn.Date.AddDays(1);

            return (await db.PhotoContents
                    .Where(x => x.PhotoCreatedOn > dateTimeAfter && x.PhotoCreatedOn < dateTimeBefore).ToListAsync())
                .Cast<object>().ToList();
        }

        public static async Task RunReport(Func<Task<List<object>>> toRun, string title)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var reportLoader = new ContentListLoaderReport(toRun, PhotoListLoader.SortContextPhotoDefault());

            var context = new PhotoListWithActionsContext(null, reportLoader);

            await ThreadSwitcher.ResumeForegroundAsync();

            var newWindow = new PhotoListWindow {PhotoListContext = context, WindowTitle = title};

            newWindow.PositionWindowAndShow();
        }


        public static async Task<List<object>> ShutterSpeedSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.ShutterSpeed == content.ShutterSpeed).ToListAsync())
                .Cast<object>().ToList();
        }


        public async Task ViewFile(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null) return;

            try
            {
                var context = await Db.Context();

                var refreshedData = context.PhotoContents.SingleOrDefault(x => x.ContentId == content.ContentId);

                var possibleFile = UserSettingsSingleton.CurrentSettings()
                    .LocalMediaArchivePhotoContentFile(refreshedData);

                if (possibleFile is not {Exists: true})
                {
                    StatusContext.ToastWarning("No Media File Found?");
                    return;
                }

                await ThreadSwitcher.ResumeForegroundAsync();

                var ps = new ProcessStartInfo(possibleFile.FullName) {UseShellExecute = true, Verb = "open"};
                Process.Start(ps);
            }
            catch (Exception e)
            {
                StatusContext.ToastWarning($"Trouble Showing Image - {e.Message}");
            }
        }
    }
}