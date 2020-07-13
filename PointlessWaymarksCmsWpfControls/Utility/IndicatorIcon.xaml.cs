using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    /// <summary>
    ///     Interaction logic for IndicatorIcon.xaml
    /// </summary>
    public partial class IndicatorIcon : UserControl
    {
        public static readonly DependencyProperty IconBrushProperty = DependencyProperty.Register("IconBrush",
            typeof(Brush), typeof(IndicatorIcon), new PropertyMetadata(default(Brush)));

        public static readonly DependencyProperty IconPathGeometryProperty =
            DependencyProperty.Register("IconPathGeometry", typeof(Geometry), typeof(IndicatorIcon),
                new PropertyMetadata(default(Geometry)));

        public static readonly DependencyProperty HoverTextProperty = DependencyProperty.Register("HoverText",
            typeof(string), typeof(IndicatorIcon), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register("IconSize",
            typeof(double), typeof(IndicatorIcon), new PropertyMetadata(default(double)));

        public IndicatorIcon()
        {
            InitializeComponent();
        }

        public string HoverText
        {
            get => (string) GetValue(HoverTextProperty);
            set => SetValue(HoverTextProperty, value);
        }

        public Brush IconBrush
        {
            get => (Brush) GetValue(IconBrushProperty);
            set => SetValue(IconBrushProperty, value);
        }

        public Geometry IconPathGeometry
        {
            get => (Geometry) GetValue(IconPathGeometryProperty);
            set => SetValue(IconPathGeometryProperty, value);
        }

        public double IconSize
        {
            get => (double) GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }
    }
}