using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ImageList;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PostList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.ContentList
{
    public class ContentListContext : INotifyPropertyChanged
    {
        private ObservableCollection<object> _items;
        private PhotoListItemActions _photoItemActions;

        public ContentListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            PhotoItemActions = new PhotoListItemActions(StatusContext);
            
            DataNotificationsProcessor = new DataNotificationsWorkQueue {Processor = DataNotificationReceived};
        }

        private Task DataNotificationReceived(TinyMessageReceivedEventArgs arg)
        {
            throw new System.NotImplementedException();
        }

        public ObservableCollection<object> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public PhotoListItemActions PhotoItemActions
        {
            get => _photoItemActions;
            set
            {
                if (Equals(value, _photoItemActions)) return;
                _photoItemActions = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            //DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;
            
            var db = await Db.Context();

            var listItems = new List<object>();

            var photoItems = db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .Take(20).ToList();
            
            listItems.AddRange(photoItems.Select(x => PhotoListItemActions.ListItemFromDbItem(x, PhotoItemActions)));
            
            var postItems = db.PostContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .Take(20).ToList();
            
            listItems.AddRange(postItems.Select(PostListContext.ListItemFromDbItem));
            
            var imageItems = db.ImageContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .Take(20).ToList();
            
            listItems.AddRange(imageItems.Select(ImageListContext.ListItemFromDbItem));
            
            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Loading Display List of Photos");

            Items = new ObservableCollection<object>(listItems);
        }

        public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }

        public StatusControlContext StatusContext { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}