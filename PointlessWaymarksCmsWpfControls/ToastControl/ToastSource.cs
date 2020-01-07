using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using JetBrains.Annotations;

namespace PointlessWaymarksCmsWpfControls.ToastControl
{
    public class ToastSource : INotifyPropertyChanged
    {
        public const int UnlimitedNotifications = -1;

        public static readonly TimeSpan NeverEndingNotification = TimeSpan.MaxValue;

        private readonly DispatcherTimer _timer;
        private bool _isOpen;

        public ToastSource() : this(Dispatcher.CurrentDispatcher)
        {
        }

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

        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                if (value == _isOpen) return;
                _isOpen = value;
                OnPropertyChanged();
            }
        }

        public long MaximumNotificationCount { get; set; }
        public TimeSpan NotificationLifeTime { get; set; }
        public ObservableCollection<ToastViewModel> NotificationMessages { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Hide(Guid id)
        {
            var n = NotificationMessages.SingleOrDefault(x => x.Id == id);
            if (n?.InvokeHideAnimation == null)
                return;

            n.InvokeHideAnimation();

            var timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(200), Tag = n};
            timer.Tick += RemoveNotificationsTimer_OnTick;
            timer.Start();
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
                    var removeCount = (int) (NotificationMessages.Count - MaximumNotificationCount) + 1;

                    var itemsToRemove = NotificationMessages.OrderBy(x => x.CreateTime).Take(removeCount)
                        .Select(x => x.Id).ToList();
                    foreach (var id in itemsToRemove)
                        Hide(id);
                }

            NotificationMessages.Add(new ToastViewModel {Message = message, Type = type});
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            if (!(sender is DispatcherTimer timer)) return;

            // Stop the timer and cleanup for GC
            timer.Tick += RemoveNotificationsTimer_OnTick;
            timer.Stop();

            if (!(timer.Tag is ToastViewModel n)) return;

            NotificationMessages.Remove(n);

            if (NotificationMessages.Any()) return;

            InternalStopTimer();
            IsOpen = false;
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
}