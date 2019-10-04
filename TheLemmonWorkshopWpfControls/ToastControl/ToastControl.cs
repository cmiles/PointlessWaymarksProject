using System.Windows;
using System.Windows.Controls;

namespace TheLemmonWorkshopWpfControls.ToastControl
{
    public class ToastControl : Control
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon",
            typeof(FrameworkElement), typeof(ToastControl), new PropertyMetadata(default(FrameworkElement)));

        public static readonly RoutedEvent NotificationClosedEvent =
            EventManager.RegisterRoutedEvent("NotificationClosed", RoutingStrategy.Direct, typeof(RoutedEventHandler),
                typeof(ToastControl));

        public static readonly RoutedEvent NotificationClosingEvent =
            EventManager.RegisterRoutedEvent("NotificationClosing", RoutingStrategy.Direct, typeof(RoutedEventHandler),
                typeof(ToastControl));

        public static readonly DependencyProperty NotificationProperty = DependencyProperty.Register("Toast",
            typeof(ToastViewModel), typeof(ToastControl),
            new PropertyMetadata(default(ToastViewModel), NotificationChanged));

        private bool _isClosed;
        private bool _isClosing;

        static ToastControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToastControl),
                new FrameworkPropertyMetadata(typeof(ToastControl)));
        }

        public ToastControl()
        {
            _isClosed = false;
            _isClosing = false;

            Unloaded += OnUnloaded;
        }

        public event RoutedEventHandler NotificationClosed
        {
            add => AddHandler(NotificationClosedEvent, value);
            remove => RemoveHandler(NotificationClosedEvent, value);
        }

        public event RoutedEventHandler NotificationClosing
        {
            add => AddHandler(NotificationClosingEvent, value);
            remove => RemoveHandler(NotificationClosingEvent, value);
        }

        public Canvas Icon
        {
            get => (Canvas)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public ToastViewModel Toast
        {
            get => (ToastViewModel)GetValue(NotificationProperty);
            set => SetValue(NotificationProperty, value);
        }

        public void InvokeHideAnimation()
        {
            if (_isClosing || _isClosed)
                return;

            _isClosing = true;

            RaiseEvent(new RoutedEventArgs(NotificationClosingEvent));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("CloseButton") is Button closeButton)
                closeButton.Click += CloseButtonClicked;
        }

        private static void NotificationChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs eventArgs)
        {
            if (!(dependencyObject is ToastControl control) ||
                !(eventArgs.NewValue is ToastViewModel notification))
                return;

            notification.InvokeHideAnimation = control.InvokeHideAnimation;
        }

        private void CloseButtonClicked(object sender, RoutedEventArgs e)
        {
            if (_isClosing || _isClosed)
                return;

            _isClosed = true;

            RaiseEvent(new RoutedEventArgs(NotificationClosedEvent));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (GetTemplateChild("CloseButton") is Button closeButton)
                closeButton.Click -= CloseButtonClicked;
        }
    }
}