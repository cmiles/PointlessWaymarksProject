using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.ListFilterBuilder;

/// <summary>
///     Interaction logic for ListFilterBuilderControl.xaml
/// </summary>
public partial class ListFilterBuilderControl : UserControl
{
    public ListFilterBuilderControl()
    {
        InitializeComponent();
    }

    private void ContentIdEntryTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var newText = Regex.Replace(textBox.Text.Replace("\n", " ").Replace("\r", " "), @"\s+", " ");
            textBox.Text = newText;
        }
    }
}