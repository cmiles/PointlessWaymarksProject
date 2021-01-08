using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;
using MvvmHelpers.Commands;

namespace PointlessWaymarks.WpfCommon.Status
{
    public class UserCancellations : INotifyPropertyChanged
    {
        private CancellationTokenSource _cancelSource;
        private string _description;
        private bool _isEnabled = true;

        public UserCancellations()
        {
            Cancel = new Command(() =>
            {
                CancelSource?.Cancel();
                IsEnabled = false;
                Description = "Canceling...";
            });
        }

        public Command Cancel { get; set; }

        public CancellationTokenSource CancelSource
        {
            get => _cancelSource;
            set
            {
                if (Equals(value, _cancelSource)) return;
                _cancelSource = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (value == _description) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value == _isEnabled) return;
                _isEnabled = value;
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