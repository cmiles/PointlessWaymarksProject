using System.Windows;

namespace PointlessWaymarks.WpfCommon.StringDataEntry;

/// <summary>
///     Interaction logic for StringDataEntryMultiLineControl.xaml
/// </summary>
public partial class StringDataEntryMultiLineControl
{
    public static readonly DependencyProperty TextBoxHeightProperty = DependencyProperty.Register("TextBoxHeight",
        typeof(double), typeof(StringDataEntryControl), new PropertyMetadata(default(double)));

    public StringDataEntryMultiLineControl()
    {
        InitializeComponent();
    }

    public double TextBoxHeight
    {
        get => (double) GetValue(TextBoxHeightProperty);
        set
        {
            {
                SetValue(TextBoxHeightProperty, value);
                ValueTextBox.Height = value;
            }
        }
    }
}