using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PointlessWaymarks.WpfCommon.Utility;

public static class TextBoxHelpers
{
    /// <summary>
    ///     Useful especially for tracking the cursor in a drag and drop scenario.
    /// </summary>
    /// <param name="textBox"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static int GetCaretIndexFromPoint(TextBox textBox, Point point)
    {
        //You can find this code in a number of answers online that - the modification here
        //to the most commonly found examples is that this supports a multi-line TextBox
        var index = textBox.GetCharacterIndexFromPoint(point, true);
        var line = textBox.GetLineIndexFromCharacterIndex(index);
        var lineLength = textBox.GetLineLength(line);

        if (index < lineLength - 1) return index;

        // Get the position of the character index using the bounding rectangle
        var caretRect = textBox.GetRectFromCharacterIndex(index);
        var caretPoint = new Point(caretRect.X, caretRect.Y);

        if (point.X > caretPoint.X) index += 1;
        return index;
    }

    /// <summary>
    ///     Insert text to the focused TextBox via an event - this will put the change on the standard undo stack.
    /// </summary>
    /// <param name="toInsert"></param>
    public static void InsertTextAtCaretAsKeyboardToFocusedTextBox(string toInsert)
    {
        //https://stackoverflow.com/questions/1645815/how-can-i-programmatically-generate-keypress-events-in-c
        var target = Keyboard.FocusedElement;
        var routedEvent = TextCompositionManager.TextInputEvent;

        target.RaiseEvent(new TextCompositionEventArgs(InputManager.Current.PrimaryKeyboardDevice,
            new TextComposition(InputManager.Current, target, toInsert)) {RoutedEvent = routedEvent});
    }
}