using System.Windows;
using System.Windows.Media;

namespace PointlessWaymarks.WpfCommon.WaitingSpinner;

/// <summary>
///     Interaction logic for WaitingSpinnerControl.xaml - code from http://blog.trustmycode.net/?p=133 and
///     https://github.com/ThomasSteinbinder/WPFAnimatedLoadingSpinner
/// </summary>
public partial class WaitingSpinnerControl
{
    public static readonly DependencyProperty DiameterProperty = DependencyProperty.Register(nameof(Diameter),
        typeof(int), typeof(WaitingSpinnerControl), new PropertyMetadata(20, OnDiameterPropertyChanged));

    public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register(nameof(Radius), typeof(int),
        typeof(WaitingSpinnerControl), new PropertyMetadata(15, null, OnCoerceRadius));

    public static readonly DependencyProperty InnerRadiusProperty = DependencyProperty.Register(nameof(InnerRadius),
        typeof(int), typeof(WaitingSpinnerControl), new PropertyMetadata(2, null, OnCoerceInnerRadius));

    public static readonly DependencyProperty CenterProperty = DependencyProperty.Register(nameof(Center), typeof(Point),
        typeof(WaitingSpinnerControl), new PropertyMetadata(new Point(15, 15), null, OnCoerceCenter));

    public static readonly DependencyProperty Color1Property = DependencyProperty.Register(nameof(Color1), typeof(Color),
        typeof(WaitingSpinnerControl), new PropertyMetadata(Colors.Green));

    public static readonly DependencyProperty Color2Property = DependencyProperty.Register(nameof(Color2), typeof(Color),
        typeof(WaitingSpinnerControl), new PropertyMetadata(Colors.Transparent));


    public WaitingSpinnerControl()
    {
        InitializeComponent();
    }

    public Point Center
    {
        get => (Point) GetValue(CenterProperty);
        set => SetValue(CenterProperty, value);
    }

    public Color Color1
    {
        get => (Color) GetValue(Color1Property);
        set => SetValue(Color1Property, value);
    }

    public Color Color2
    {
        get => (Color) GetValue(Color2Property);
        set => SetValue(Color2Property, value);
    }

    public int Diameter
    {
        get => (int) GetValue(DiameterProperty);
        set
        {
            if (value < 10)
                value = 10;
            SetValue(DiameterProperty, value);
        }
    }

    public int InnerRadius
    {
        get => (int) GetValue(InnerRadiusProperty);
        set => SetValue(InnerRadiusProperty, value);
    }

    public int Radius
    {
        get => (int) GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    private static object OnCoerceCenter(DependencyObject d, object baseValue)
    {
        var control = (WaitingSpinnerControl) d;
        var newCenter = (int) control.GetValue(DiameterProperty) / 2;
        return new Point(newCenter, newCenter);
    }

    private static object OnCoerceInnerRadius(DependencyObject d, object baseValue)
    {
        var control = (WaitingSpinnerControl) d;
        var newInnerRadius = (int) control.GetValue(DiameterProperty) / 4;
        return newInnerRadius;
    }

    private static object OnCoerceRadius(DependencyObject d, object baseValue)
    {
        var control = (WaitingSpinnerControl) d;
        var newRadius = (int) control.GetValue(DiameterProperty) / 2;
        return newRadius;
    }

    private static void OnDiameterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        d.CoerceValue(CenterProperty);
        d.CoerceValue(RadiusProperty);
        d.CoerceValue(InnerRadiusProperty);
    }
}