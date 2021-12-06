using System.Windows;

namespace PointlessWaymarks.WpfCommon.ToastControl;

public partial class ToastTray
{
    public ToastTray()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void Notification_OnNotificationClosed(object sender, RoutedEventArgs e)
    {
        if (sender is not ToastControl control)
            return;

        (DataContext as ToastSource)?.Hide(control.Toast.Id);
    }
}