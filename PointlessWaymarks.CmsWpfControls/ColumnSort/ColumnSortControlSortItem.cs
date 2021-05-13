using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarks.CmsWpfControls.ColumnSort
{
    public class ColumnSortControlSortItem : INotifyPropertyChanged
    {
        private bool _ascending;
        private string _columnName;
        private string _displayName;
        private int _order;

        public bool Ascending
        {
            get => _ascending;
            set
            {
                if (value == _ascending) return;
                _ascending = value;
                OnPropertyChanged();
            }
        }

        public string ColumnName
        {
            get => _columnName;
            set
            {
                if (value == _columnName) return;
                _columnName = value;
                OnPropertyChanged();
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
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