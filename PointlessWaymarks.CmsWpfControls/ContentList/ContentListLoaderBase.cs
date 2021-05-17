using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData;

namespace PointlessWaymarks.CmsWpfControls.ContentList
{
    public abstract class ContentListLoaderBase : IContentListLoader
    {
        private bool _addNewItemsFromDataNotifications;
        private bool _allItemsLoaded;

        private List<DataNotificationContentType> _dataNotificationTypesToRespondTo =
            new();

        private int? _partialLoadQuantity;

        public ContentListLoaderBase(int? partialLoadQuantity)
        {
            PartialLoadQuantity = partialLoadQuantity;
        }

        public bool AddNewItemsFromDataNotifications
        {
            get => _addNewItemsFromDataNotifications;
            set
            {
                if (value == _addNewItemsFromDataNotifications) return;
                _addNewItemsFromDataNotifications = value;
                OnPropertyChanged();
            }
        }

        public bool AllItemsLoaded
        {
            get => _allItemsLoaded;
            set
            {
                if (value == _allItemsLoaded) return;
                _allItemsLoaded = value;
                OnPropertyChanged();
            }
        }

        public abstract Task<bool> CheckAllItemsAreLoaded();

        public List<DataNotificationContentType> DataNotificationTypesToRespondTo
        {
            get => _dataNotificationTypesToRespondTo;
            set
            {
                if (Equals(value, _dataNotificationTypesToRespondTo)) return;
                _dataNotificationTypesToRespondTo = value;
                OnPropertyChanged();
            }
        }

        public abstract Task<List<object>> LoadItems(IProgress<string> progress = null);

        public int? PartialLoadQuantity
        {
            get => _partialLoadQuantity;
            set
            {
                if (value == _partialLoadQuantity) return;
                _partialLoadQuantity = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}