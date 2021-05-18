using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.PhotoList
{
    public class PhotoListItemActions
    {
        private Command<PhotoContent> _apertureSearchCommand;
        private Command<PhotoContent> _cameraMakeSearchCommand;
        private Command<PhotoContent> _cameraModelSearchCommand;

        private Command<PhotoContent> _editContentCommand;
        private Command<PhotoContent> _focalLengthSearchCommand;
        private Command<PhotoContent> _isoSearchCommand;

        private Command<PhotoContent> _lensSearchCommand;
        private Command<PhotoContent> _photoTakenOnSearchCommand;
        private Command<PhotoContent> _shutterSpeedSearchCommand;
        private StatusControlContext _statusContext;
        private Command<PhotoContent> _viewFileCommand;

        public PhotoListItemActions(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            ViewFileCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(ViewImage);
            EditContentCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(EditContent);
            ApertureSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await ApertureSearch(x), $"Aperture - {x.Aperture}"));
            LensSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await LensSearch(x), $"Lens - {x.Lens}"));
            CameraMakeSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await CameraMakeSearch(x), $"Camera Make - {x.CameraMake}"));
            CameraModelSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await CameraModelSearch(x), $"Camera Model - {x.CameraModel}"));
            FocalLengthSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await FocalLengthSearch(x), $"Focal Length - {x.FocalLength}"));
            IsoSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await IsoSearch(x), $"ISO - {x.Iso}"));
            ShutterSpeedSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await ShutterSpeedSearch(x), $"Shutter Speed - {x.ShutterSpeed}"));
            PhotoTakenOnSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
                await RunReport(async () => await PhotoTakenOnSearch(x),
                    $"Photo Created On - {x.PhotoCreatedOn.Date:D}"));
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

        public Command<PhotoContent> EditContentCommand
        {
            get => _editContentCommand;
            set
            {
                if (Equals(value, _editContentCommand)) return;
                _editContentCommand = value;
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

        private static async Task<List<object>> ApertureSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.Aperture == content.Aperture).ToListAsync()).Cast<object>()
                .ToList();
        }

        private static async Task<List<object>> CameraMakeSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.CameraMake == content.CameraMake).ToListAsync()).Cast<object>()
                .ToList();
            ;
        }

        private static async Task<List<object>> CameraModelSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.CameraModel == content.CameraModel).ToListAsync())
                .Cast<object>().ToList();
            ;
        }

        private async Task EditContent(PhotoContent content)
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


        private static async Task<List<object>> FocalLengthSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.FocalLength == content.FocalLength).ToListAsync())
                .Cast<object>().ToList();
            ;
        }

        private static async Task<List<object>> IsoSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.Iso == content.Iso).ToListAsync()).Cast<object>().ToList();
            ;
        }

        private static async Task<List<object>> LensSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.Lens == content.Lens).ToListAsync()).Cast<object>().ToList();
            ;
        }

        public static PhotoListListItem ListItemFromDbItem(PhotoContent content,
            PhotoListItemActions photoListItemActions)
        {
            return new()
            {
                DbEntry = content, SmallImageUrl = ContentListContext.GetSmallImageUrl(content),
                ItemActions = photoListItemActions
            };
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static async Task<List<object>> PhotoTakenOnSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            //Todo: I think this should be possible via something like DbFunctions or EF functions?
            //I didn't understand what approach to take from a few google searches...
            var dateTimeAfter = content.PhotoCreatedOn.Date.AddDays(-1);
            var dateTimeBefore = content.PhotoCreatedOn.Date.AddDays(1);

            return (await db.PhotoContents
                    .Where(x => x.PhotoCreatedOn > dateTimeAfter && x.PhotoCreatedOn < dateTimeBefore).ToListAsync())
                .Cast<object>().ToList();
            ;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private static async Task RunReport(Func<Task<List<object>>> toRun, string title)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var reportLoader = new ContentListLoaderReport(toRun);

            var context = new PhotoListWithActionsContext(null, reportLoader);

            await ThreadSwitcher.ResumeForegroundAsync();

            var newWindow = new PhotoListWindow {PhotoListContext = context, WindowTitle = title};

            newWindow.PositionWindowAndShow();
        }


        private static async Task<List<object>> ShutterSpeedSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return (await db.PhotoContents.Where(x => x.ShutterSpeed == content.ShutterSpeed).ToListAsync())
                .Cast<object>().ToList();
            ;
        }


        private async Task ViewImage(PhotoContent content)
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