using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;
using PointlessWaymarks.WpfCommon.Commands;

namespace PointlessWaymarks.CmsWpfControls.Utility
{
    public class CurrentSelectedTextTracker : INotifyPropertyChanged
    {
        private string _currentSelectedText;
        private Command<RoutedEventArgs> _selectedTextChangedCommand;

        public CurrentSelectedTextTracker()
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

        public Command<RoutedEventArgs> SelectedTextChangedCommand
        {
            get => _selectedTextChangedCommand;
            set
            {
                if (Equals(value, _selectedTextChangedCommand)) return;
                _selectedTextChangedCommand = value;
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