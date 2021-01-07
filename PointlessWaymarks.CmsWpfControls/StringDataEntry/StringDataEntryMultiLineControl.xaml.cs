using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.StringDataEntry
{
    /// <summary>
    ///     Interaction logic for StringDataEntryMultiLineControl.xaml
    /// </summary>
    public partial class StringDataEntryMultiLineControl : UserControl
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
}