using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.WpfCommon.ToastControl;

public partial class ToastSource : ObservableObject
{
    public const int UnlimitedNotifications = -1;
    public static readonly TimeSpan NeverEndingNotification = TimeSpan.MaxValue;
    private readonly DispatcherTimer _timer;

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private TimeSpan _notificationLifeTime;
    [ObservableProperty] private int _maximumNotificationCount;

    public ToastSource(Dispatcher dispatcher)
    {
        NotificationMessages = new ObservableCollection<ToastViewModel>();

        MaximumNotificationCount = 5;
        NotificationLifeTime = TimeSpan.FromSeconds(6);

        _timer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
    }

    public ObservableCollection<ToastViewModel> NotificationMessages { get; }

    public void Hide(Guid id)
    {
        var n = NotificationMessages.SingleOrDefault(x => x.Id == id);
        if (n?.InvokeHideAnimation == null)
            return;

        n.InvokeHideAnimation();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200), Tag = n };
        timer.Tick += RemoveNotificationsTimer_OnTick;
        timer.Start();
    }

    private void InternalStartTimer()
    {
        _timer.Tick += TimerOnTick;
        _timer.Start();
    }

    private void InternalStopTimer()
    {
        _timer.Stop();
        _timer.Tick -= TimerOnTick;
    }

    private void RemoveNotificationsTimer_OnTick(object sender, EventArgs eventArgs)
    {
        if (sender is not DispatcherTimer timer) return;

        // Stop the timer and cleanup for GC
        timer.Tick += RemoveNotificationsTimer_OnTick;
        timer.Stop();

        if (timer.Tag is not ToastViewModel n) return;

        NotificationMessages.Remove(n);

        if (NotificationMessages.Any()) return;

        InternalStopTimer();
        IsOpen = false;
    }

    public void Show(string message, ToastType type)
    {
        if (NotificationMessages.Any() == false)
        {
            InternalStartTimer();
            IsOpen = true;
        }

        if (MaximumNotificationCount != UnlimitedNotifications)
            if (NotificationMessages.Count >= MaximumNotificationCount)
            {
                var removeCount = NotificationMessages.Count - MaximumNotificationCount + 1;

                var itemsToRemove = NotificationMessages.OrderBy(x => x.CreateTime).Take(removeCount).Select(x => x.Id)
                    .ToList();
                foreach (var id in itemsToRemove)
                    Hide(id);
            }

        NotificationMessages.Add(new ToastViewModel { Message = message, Type = type });
    }

    private void TimerOnTick(object sender, EventArgs eventArgs)
    {
        if (NotificationLifeTime == NeverEndingNotification)
            return;

        var currentTime = DateTime.Now;
        var itemsToRemove = NotificationMessages.Where(x => currentTime - x.CreateTime >= NotificationLifeTime)
            .Select(x => x.Id).ToList();

        foreach (var id in itemsToRemove) Hide(id);
    }
}