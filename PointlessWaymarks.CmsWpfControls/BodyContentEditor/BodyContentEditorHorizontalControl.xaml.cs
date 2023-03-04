using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.BodyContentEditor;

/// <summary>
///     Interaction logic for BodyContentEditorHorizontalControl.xaml
/// </summary>
public partial class BodyContentEditorHorizontalControl
{
    public BodyContentEditorHorizontalControl()
    {
        InitializeComponent();
    }

    private void BodyContentTextBox_OnSelectionChanged(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not BodyContentEditorContext context) return;
        if (sender is not TextBox t) return;

        context.UserBodyContentUserSelectionStart = t.SelectionStart;
        context.UserBodyContentUserSelectionLength = t.SelectionLength;
    }

    private void TextBoxPreviewDragOver(object? sender, DragEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        // Set the caret at the position where user ended the drag-drop operation
        var dropPosition = e.GetPosition(textBox);
        textBox.SelectionStart = TextBoxHelpers.GetCaretIndexFromPoint(textBox, dropPosition);
        textBox.SelectionLength = 0;

        Debug.WriteLine($"{textBox.SelectionStart}");

        // don't forget to set focus to the text box to make the caret visible!
        textBox.Focus();
        e.Handled = true;
    }
}