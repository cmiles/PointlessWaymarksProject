using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.WpfCommon.Commands;

namespace PointlessWaymarks.CmsWpfControls.PhotoList
{
    public class PhotoListListItem : INotifyPropertyChanged
    {
        private string _currentSelectedText;

        private PhotoContent _dbEntry;
        private string _smallImageUrl;

        public PhotoListListItem()
        {
            SelectedTextChangedCommand = new Command<RoutedEventArgs>(SelectedTextChanged);
        }

        public string CurrentSelectedText
        {
            get => _currentSelectedText;
            set
            {
                if (value == _currentSelectedText) return;
                _currentSelectedText = value;
                OnPropertyChanged();
            }
        }

        public PhotoContent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public Command<RoutedEventArgs> SelectedTextChangedCommand { get; set; }

        public string SmallImageUrl
        {
            get => _smallImageUrl;
            set
            {
                if (value == _smallImageUrl) return;
                _smallImageUrl = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SelectedTextChanged(RoutedEventArgs obj)
        {
            var source = obj.Source as TextBox;
            CurrentSelectedText = source?.SelectedText;
        }
    }
}