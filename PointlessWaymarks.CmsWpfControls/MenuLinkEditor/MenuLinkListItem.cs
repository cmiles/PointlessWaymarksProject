using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.MenuLinkEditor
{
    public class MenuLinkListItem : INotifyPropertyChanged
    {
        private MenuLink _dbEntry;
        private bool _hasChanges;
        private string _userLink;
        private int _userOrder;

        public MenuLink DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();

                if (DbEntry == null)
                {
                    UserLink = string.Empty;
                    UserOrder = 0;
                }
                else
                {
                    UserLink = (DbEntry.LinkTag ?? string.Empty).Trim();
                    UserOrder = DbEntry.MenuOrder;
                }
            }
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (value == _hasChanges) return;
                _hasChanges = value;
                OnPropertyChanged();
            }
        }

        public string UserLink
        {
            get => _userLink;
            set
            {
                if (value == _userLink) return;
                _userLink = value;
                OnPropertyChanged();
            }
        }

        public int UserOrder
        {
            get => _userOrder;
            set
            {
                if (value == _userOrder) return;
                _userOrder = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void CheckForChanges()
        {
            if (DbEntry == null || DbEntry.Id < 1)
            {
                HasChanges = true;
                return;
            }

            HasChanges = CleanedUserLink() != DbEntry.LinkTag || UserOrder != DbEntry.MenuOrder;
        }

        private string CleanedUserLink()
        {
            var toReturn = UserLink ?? string.Empty;

            return toReturn.Trim();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation")) CheckForChanges();
        }
    }
}