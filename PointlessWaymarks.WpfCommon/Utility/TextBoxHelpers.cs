using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.WpfCommon.Utility
{
    public static class TextBoxHelpers
    {
        public static int GetCaretIndexFromPoint(TextBox textBox, Point point)
        {
            var index = textBox.GetCharacterIndexFromPoint(point, true);

            // GetCharacterIndexFromPoint is missing one caret position, as there is one extra caret position than there are characters (an extra one at the end).
            //  We have to add that caret index if the given point is at the end of the TextBox
            if (index != textBox.Text.Length - 1) return index;

            // Get the position of the character index using the bounding rectangle
            var caretRect = textBox.GetRectFromCharacterIndex(index);
            var caretPoint = new Point(caretRect.X, caretRect.Y);

            if (point.X > caretPoint.X) index += 1;
            return index;
        }
    }
}