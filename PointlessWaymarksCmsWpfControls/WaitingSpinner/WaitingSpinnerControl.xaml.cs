using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PointlessWaymarksCmsWpfControls.WaitingSpinner
{
    /// <summary>
    /// Interaction logic for WaitingSpinnerControl.xaml - code from http://blog.trustmycode.net/?p=133 and https://github.com/ThomasSteinbinder/WPFAnimatedLoadingSpinner
    /// </summary>
    public partial class WaitingSpinnerControl : UserControl
    {
        public WaitingSpinnerControl()
        {
            InitializeComponent();
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

        public static readonly DependencyProperty DiameterProperty = DependencyProperty.Register("Diameter",
            typeof(int), typeof(WaitingSpinnerControl), new PropertyMetadata(20, OnDiameterPropertyChanged));

        private static void OnDiameterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(CenterProperty);
            d.CoerceValue(RadiusProperty);
            d.CoerceValue(InnerRadiusProperty);
        }

        public int Radius
        {
            get => (int) GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
        }

        public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof(int),
            typeof(WaitingSpinnerControl), new PropertyMetadata(15, null, OnCoerceRadius));

        private static object OnCoerceRadius(DependencyObject d, object baseValue)
        {
            var control = (WaitingSpinnerControl) d;
            var newRadius = (int) control.GetValue(DiameterProperty) / 2;
            return newRadius;
        }

        public int InnerRadius
        {
            get => (int) GetValue(InnerRadiusProperty);
            set => SetValue(InnerRadiusProperty, value);
        }

        public static readonly DependencyProperty InnerRadiusProperty = DependencyProperty.Register("InnerRadius",
            typeof(int), typeof(WaitingSpinnerControl), new PropertyMetadata(2, null, OnCoerceInnerRadius));

        private static object OnCoerceInnerRadius(DependencyObject d, object baseValue)
        {
            var control = (WaitingSpinnerControl) d;
            var newInnerRadius = (int) (control.GetValue(DiameterProperty)) / 4;
            return newInnerRadius;
        }

        public Point Center
        {
            get => (Point) GetValue(CenterProperty);
            set => SetValue(CenterProperty, value);
        }

        public static readonly DependencyProperty CenterProperty = DependencyProperty.Register("Center", typeof(Point),
            typeof(WaitingSpinnerControl), new PropertyMetadata(new Point(15, 15), null, OnCoerceCenter));

        private static object OnCoerceCenter(DependencyObject d, object baseValue)
        {
            var control = (WaitingSpinnerControl) d;
            var newCenter = (int) control.GetValue(DiameterProperty) / 2;
            return new Point(newCenter, newCenter);
        }

        public Color Color1
        {
            get => (Color) GetValue(Color1Property);
            set => SetValue(Color1Property, value);
        }

        public static readonly DependencyProperty Color1Property = DependencyProperty.Register("Color1", typeof(Color),
            typeof(WaitingSpinnerControl), new PropertyMetadata(Colors.Green));

        public Color Color2
        {
            get => (Color) GetValue(Color2Property);
            set => SetValue(Color2Property, value);
        }

        public static readonly DependencyProperty Color2Property = DependencyProperty.Register("Color2", typeof(Color),
            typeof(WaitingSpinnerControl), new PropertyMetadata(Colors.Transparent));
    }
}