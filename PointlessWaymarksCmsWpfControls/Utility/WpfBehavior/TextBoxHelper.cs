using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarksCmsWpfControls.Utility.WpfBehavior
{
    //From https://stackoverflow.com/questions/2245928/mvvm-and-the-textboxs-selectedtext-property
    public static class TextBoxHelper
    {
        // Using a DependencyProperty as the backing store for SelectedText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedTextProperty =
            DependencyProperty.RegisterAttached("SelectedText", typeof(string), typeof(TextBoxHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    SelectedTextChanged));

        public static string GetSelectedText(DependencyObject obj)
        {
            return (string) obj.GetValue(SelectedTextProperty);
        }

        private static void SelectedTextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is TextBox tb)) return;

            if (e.OldValue == null && e.NewValue != null)
                tb.SelectionChanged += tb_SelectionChanged;
            else if (e.OldValue != null && e.NewValue == null) tb.SelectionChanged -= tb_SelectionChanged;

            if (e.NewValue is string newValue && newValue != tb.SelectedText) tb.SelectedText = newValue;
        }

        public static void SetSelectedText(DependencyObject obj, string value)
        {
            obj.SetValue(SelectedTextProperty, value);
        }

        private static void tb_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) SetSelectedText(tb, tb.SelectedText);
        }
    }
}