using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarks.CmsWpfControls.ColumnSort
{
    public class ColumnSortControlSortItem : INotifyPropertyChanged
    {
        private string _columnName;
        private string _displayName;
        private int _order;
        private ListSortDirection _defaultSortDirection = ListSortDirection.Ascending;
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;

        public ListSortDirection DefaultSortDirection
        {
            get => _defaultSortDirection;
            set
            {
                if (value == _defaultSortDirection) return;
                _defaultSortDirection = value;
                OnPropertyChanged();
                SortDirection = DefaultSortDirection;
            }
        }

        public ListSortDirection SortDirection
        {
            get => _sortDirection;
            set
            {
                if (value == _sortDirection) return;
                _sortDirection = value;
                OnPropertyChanged();
            }
        }

        public string ColumnName
        {
            get => _columnName;
            init
            {
                if (value == _columnName) return;
                _columnName = value;
                OnPropertyChanged();
            }
        }

        public string DisplayName
        {
            get => _displayName;
            init
            {
                if (value == _displayName) return;
                _displayName = value;
                OnPropertyChanged();
            }
        }

        public int Order
        {
            get => _order;
            set
            {
                if (value == _order) return;
                _order = value;
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