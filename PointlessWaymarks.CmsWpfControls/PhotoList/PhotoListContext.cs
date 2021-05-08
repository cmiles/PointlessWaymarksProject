using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.PhotoList
{
    public class PhotoListContext : INotifyPropertyChanged
    {
        public enum PhotoListLoadMode
        {
            Recent,
            All,
            ReportQuery
        }

        private Command<PhotoContent> _apertureSearchCommand;
        private Command<PhotoContent> _cameraMakeSearchCommand;
        private Command<PhotoContent> _cameraModelSearchCommand;

        private DataNotificationsWorkQueue _dataNotificationsProcessor;
        private Command<PhotoContent> _editContentCommand;
        private Command<PhotoContent> _focalLengthSearchCommand;
        private Command<PhotoContent> _isoSearchCommand;

        private ObservableCollection<PhotoListListItem> _items;
        private string _lastSortColumn = "CreatedOn";
        private Command<PhotoContent> _lensSearchCommand;
        private ContentListSelected<PhotoListListItem> _listSelection;
        private PhotoListLoadMode _loadMode = PhotoListLoadMode.Recent;
        private Command<PhotoContent> _photoTakenOnSearchCommand;
        private Func<Task<List<PhotoContent>>> _reportGenerator;
        private Command<PhotoContent> _shutterSpeedSearchCommand;
        private bool _sortDescending = true;
        private Command<string> _sortListCommand;
        private StatusControlContext _statusContext;
        private Command _toggleListSortDirectionCommand;
        private Command _toggleLoadRecentLoadAllCommand;
        private string _userFilterText;
        private Command<PhotoContent> _viewFileCommand;

        public PhotoListContext(StatusControlContext statusContext, PhotoListLoadMode photoListLoadMode)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            DataNotificationsProcessor = new DataNotificationsWorkQueue {Processor = DataNotificationReceived};

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

            SortListCommand = StatusContext.RunNonBlockingTaskCommand<string>(SortList);
            ToggleListSortDirectionCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
            {
                SortDescending = !SortDescending;
                await SortList(_lastSortColumn);
            });

            ToggleLoadRecentLoadAllCommand = new Command(() =>
            {
                if (LoadMode == PhotoListLoadMode.All)
                {
                    LoadMode = PhotoListLoadMode.Recent;
                    StatusContext.RunBlockingTask(LoadData);
                }
                else if (LoadMode == PhotoListLoadMode.Recent)
                {
                    LoadMode = PhotoListLoadMode.All;
                    StatusContext.RunBlockingTask(LoadData);
                }
            });

            LoadMode = photoListLoadMode;
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

        public DataNotificationsWorkQueue DataNotificationsProcessor
        {
            get => _dataNotificationsProcessor;
            set
            {
                if (Equals(value, _dataNotificationsProcessor)) return;
                _dataNotificationsProcessor = value;
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

        public ObservableCollection<PhotoListListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
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

        public ContentListSelected<PhotoListListItem> ListSelection
        {
            get => _listSelection;
            set
            {
                if (Equals(value, _listSelection)) return;
                _listSelection = value;
                OnPropertyChanged();
            }
        }

        public PhotoListLoadMode LoadMode
        {
            get => _loadMode;
            set
            {
                if (value == _loadMode) return;
                _loadMode = value;
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

        public Func<Task<List<PhotoContent>>> ReportGenerator
        {
            get => _reportGenerator;
            set
            {
                if (Equals(value, _reportGenerator)) return;
                _reportGenerator = value;
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

        public bool SortDescending
        {
            get => _sortDescending;
            set
            {
                if (value == _sortDescending) return;
                _sortDescending = value;
                OnPropertyChanged();
            }
        }

        public Command<string> SortListCommand
        {
            get => _sortListCommand;
            set
            {
                if (Equals(value, _sortListCommand)) return;
                _sortListCommand = value;
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

        public Command ToggleListSortDirectionCommand
        {
            get => _toggleListSortDirectionCommand;
            set
            {
                if (Equals(value, _toggleListSortDirectionCommand)) return;
                _toggleListSortDirectionCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ToggleLoadRecentLoadAllCommand
        {
            get => _toggleLoadRecentLoadAllCommand;
            set
            {
                if (Equals(value, _toggleLoadRecentLoadAllCommand)) return;
                _toggleLoadRecentLoadAllCommand = value;
                OnPropertyChanged();
            }
        }

        public string UserFilterText
        {
            get => _userFilterText;
            set
            {
                if (value == _userFilterText) return;
                _userFilterText = value;
                OnPropertyChanged();

                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(FilterList);
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

        public event PropertyChangedEventHandler PropertyChanged;

        private static async Task<List<PhotoContent>> ApertureSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return await db.PhotoContents.Where(x => x.Aperture == content.Aperture).ToListAsync();
        }

        private static async Task<List<PhotoContent>> CameraMakeSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return await db.PhotoContents.Where(x => x.CameraMake == content.CameraMake).ToListAsync();
        }

        private static async Task<List<PhotoContent>> CameraModelSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return await db.PhotoContents.Where(x => x.CameraModel == content.CameraModel).ToListAsync();
        }

        private async Task DataNotificationReceived(TinyMessageReceivedEventArgs e)
        {
            var translatedMessage = DataNotifications.TranslateDataNotification(e.Message);

            if (translatedMessage.HasError)
            {
                Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}",
                    translatedMessage.ErrorNote, StatusContext.StatusControlContextId);
                return;
            }

            if (translatedMessage.ContentType != DataNotificationContentType.Photo) return;

            await ThreadSwitcher.ResumeBackgroundAsync();

            if (translatedMessage.UpdateType == DataNotificationUpdateType.Delete)
            {
                var toRemove = Items.Where(x => translatedMessage.ContentIds.Contains(x.DbEntry.ContentId)).ToList();

                await ThreadSwitcher.ResumeForegroundAsync();

                toRemove.ForEach(x => Items.Remove(x));

                return;
            }

            var context = await Db.Context();

            var dbItems =
                (await context.PhotoContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).Select(ListItemFromDbItem).ToList();

            if (!dbItems.Any()) return;

            var listItems = Items.Where(x => translatedMessage.ContentIds.Contains(x.DbEntry.ContentId)).ToList();

            foreach (var loopItems in dbItems)
            {
                var existingItems = listItems.Where(x => x.DbEntry.ContentId == loopItems.DbEntry.ContentId).ToList();

                if (existingItems.Count > 1)
                {
                    await ThreadSwitcher.ResumeForegroundAsync();

                    foreach (var loopDelete in existingItems.Skip(1).ToList()) Items.Remove(loopDelete);

                    await ThreadSwitcher.ResumeBackgroundAsync();
                }

                var existingItem = existingItems.FirstOrDefault();

                if (existingItem == null)
                {
                    if (LoadMode == PhotoListLoadMode.ReportQuery) continue;

                    await ThreadSwitcher.ResumeForegroundAsync();

                    Items.Add(loopItems);

                    await ThreadSwitcher.ResumeBackgroundAsync();

                    continue;
                }

                if (translatedMessage.UpdateType == DataNotificationUpdateType.Update)
                    existingItem.DbEntry = loopItems.DbEntry;

                existingItem.SmallImageUrl = GetSmallImageUrl(existingItem.DbEntry);
            }

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(FilterList);
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


        private async Task FilterList()
        {
            if (Items == null || !Items.Any()) return;

            await ThreadSwitcher.ResumeForegroundAsync();

            ((CollectionView) CollectionViewSource.GetDefaultView(Items)).Filter = o =>
            {
                if (string.IsNullOrWhiteSpace(UserFilterText)) return true;

                var loweredString = UserFilterText.ToLower();

#pragma warning disable IDE0083 // Use pattern matching
                if (!(o is PhotoListListItem pi)) return false;
#pragma warning restore IDE0083 // Use pattern matching
                if ((pi.DbEntry.Title ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Tags ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Summary ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.CreatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.LastUpdatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.CameraMake ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.CameraModel ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Aperture ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.FocalLength ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Lens ?? string.Empty).ToLower().Contains(loweredString)) return true;
                return false;
            };
        }

        private static async Task<List<PhotoContent>> FocalLengthSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return await db.PhotoContents.Where(x => x.FocalLength == content.FocalLength).ToListAsync();
        }

        public static string GetSmallImageUrl(PhotoContent content)
        {
            if (content == null) return null;

            string smallImageUrl;

            try
            {
                smallImageUrl = PictureAssetProcessing.ProcessPhotoDirectory(content).SmallPicture?.File.FullName;
            }
            catch
            {
                smallImageUrl = null;
            }

            return smallImageUrl;
        }

        private static async Task<List<PhotoContent>> IsoSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return await db.PhotoContents.Where(x => x.Iso == content.Iso).ToListAsync();
        }

        private static async Task<List<PhotoContent>> LensSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return await db.PhotoContents.Where(x => x.Lens == content.Lens).ToListAsync();
        }

        public static PhotoListListItem ListItemFromDbItem(PhotoContent content)
        {
            return new() {DbEntry = content, SmallImageUrl = GetSmallImageUrl(content)};
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

            ListSelection = await ContentListSelected<PhotoListListItem>.CreateInstance(StatusContext);

            StatusContext.Progress("Connecting to DB");

            var db = await Db.Context();

            StatusContext.Progress("Getting Photo Db Entries");

            var dbItems = LoadMode switch
            {
                PhotoListLoadMode.Recent => db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(20).ToList(),
                PhotoListLoadMode.All => db.PhotoContents.ToList(),
                PhotoListLoadMode.ReportQuery => ReportGenerator == null
                    ? new List<PhotoContent>()
                    : await ReportGenerator(),
                _ => throw new ArgumentOutOfRangeException()
            };

            var listItems = new List<PhotoListListItem>();

            var totalCount = dbItems.Count;
            var currentLoop = 1;

            foreach (var loopItems in dbItems)
            {
                if (currentLoop == 1 || currentLoop % 25 == 0)
                    StatusContext.Progress($"Processing Photo Item {currentLoop} of {totalCount}");

                listItems.Add(ListItemFromDbItem(loopItems));

                currentLoop++;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Loading Display List of Photos");

            Items = new ObservableCollection<PhotoListListItem>(listItems);
            await SortList(_lastSortColumn);
            await FilterList();

            DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
        }

        private void OnDataNotificationReceived(object sender, TinyMessageReceivedEventArgs e)
        {
            DataNotificationsProcessor.Enqueue(e);
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static async Task<List<PhotoContent>> PhotoTakenOnSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            //Todo: I think this should be possible via something like DbFunctions or EF functions?
            //I didn't understand what approach to take from a few google searches...
            var dateTimeAfter = content.PhotoCreatedOn.Date.AddDays(-1);
            var dateTimeBefore = content.PhotoCreatedOn.Date.AddDays(1);

            return await db.PhotoContents
                .Where(x => x.PhotoCreatedOn > dateTimeAfter && x.PhotoCreatedOn < dateTimeBefore).ToListAsync();
        }


        private static async Task RunReport(Func<Task<List<PhotoContent>>> toRun, string title)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var context = new PhotoListWithActionsContext(null, toRun);

            await ThreadSwitcher.ResumeForegroundAsync();

            var newWindow = new PhotoListWindow {PhotoListContext = context, WindowTitle = title};

            newWindow.PositionWindowAndShow();
        }

        private static async Task<List<PhotoContent>> ShutterSpeedSearch(PhotoContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            return await db.PhotoContents.Where(x => x.ShutterSpeed == content.ShutterSpeed).ToListAsync();
        }

        private async Task SortList(string sortColumn)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            _lastSortColumn = sortColumn;

            var collectionView = (CollectionView) CollectionViewSource.GetDefaultView(Items);
            collectionView.SortDescriptions.Clear();

            if (string.IsNullOrWhiteSpace(sortColumn)) return;
            collectionView.SortDescriptions.Add(new SortDescription($"DbEntry.{sortColumn}",
                SortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending));
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