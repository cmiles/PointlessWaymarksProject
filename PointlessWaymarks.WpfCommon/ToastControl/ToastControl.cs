using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.WpfCommon.ToastControl;

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

    public static readonly DependencyProperty NotificationProperty = DependencyProperty.Register(nameof(Toast),
        typeof(ToastContext), typeof(ToastControl),
        new PropertyMetadata(default(ToastContext), NotificationChanged));

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

    public Canvas Icon
    {
        get => (Canvas) GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public ToastContext Toast
    {
        get => (ToastContext) GetValue(NotificationProperty);
        set => SetValue(NotificationProperty, value);
    }

    private void CloseButtonClicked(object sender, RoutedEventArgs e)
    {
        if (_isClosing || _isClosed)
            return;

        _isClosed = true;

        RaiseEvent(new RoutedEventArgs(NotificationClosedEvent));
    }

    public void InvokeHideAnimation()
    {
        if (_isClosing || _isClosed)
            return;

        _isClosing = true;

        RaiseEvent(new RoutedEventArgs(NotificationClosingEvent));
    }

    private static void NotificationChanged(DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not ToastControl control || eventArgs.NewValue is not ToastContext notification)
            return;

        notification.InvokeHideAnimation = control.InvokeHideAnimation;
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

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("CloseButton") is Button closeButton)
            closeButton.Click += CloseButtonClicked;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (GetTemplateChild("CloseButton") is Button closeButton)
            closeButton.Click -= CloseButtonClicked;
    }
}