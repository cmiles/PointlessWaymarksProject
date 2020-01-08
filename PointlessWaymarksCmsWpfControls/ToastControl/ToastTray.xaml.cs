using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;

namespace PointlessWaymarksCmsWpfControls.ToastControl
{
    public partial class ToastTray : INotifyPropertyChanged
    {
        public ToastTray()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Notification_OnNotificationClosed(object sender, RoutedEventArgs e)
        {
            if (!(sender is ToastControl control))
                return;

            (DataContext as ToastSource)?.Hide(control.Toast.Id);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}