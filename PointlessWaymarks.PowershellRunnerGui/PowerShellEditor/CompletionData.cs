using System.Management.Automation;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace PointlessWaymarks.PowerShellRunnerGui.PowerShellEditor;

//Code copied from or based on [GitHub - dfinke/PowerShellConsole: Create a PowerShell Console using the AvalonEdit control](https://github.com/dfinke/PowerShellConsole/tree/master)
//[dfinke (Doug Finke)](https://github.com/dfinke) -  Apache-2.0 license 
public class CompletionData : ICompletionData
{
    public string? CompletionText { get; set; }
    public int ReplacementLength { get; set; }
    public CompletionResultType ResulType { get; set; }
    public string? ToolTip { get; set; }

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        var length = textArea.Caret.Offset;
        var offset = completionSegment.Offset - ReplacementLength;

        length = offset == 0 ? length : length - completionSegment.Offset + ReplacementLength;

        textArea.Document.Replace(offset, length, Text);
    }

    // Use this property if you want to show a fancy UIElement in the dropdown list.
    public object? Content => Text;

    public object? Description => ToolTip;

    public ImageSource? Image
    {
        get
        {
            switch (ResulType)
            {
                case CompletionResultType.Command:
                    break;
                case CompletionResultType.History:
                    break;
                case CompletionResultType.Method:
                    break;
                case CompletionResultType.Namespace:
                    break;
                case CompletionResultType.ParameterName:
                    break;
                case CompletionResultType.ParameterValue:
                    break;
                case CompletionResultType.Property:
                    break;
                case CompletionResultType.ProviderContainer:
                    break;
                case CompletionResultType.ProviderItem:
                    break;
                case CompletionResultType.Text:
                    break;
                case CompletionResultType.Type:
                    break;
                case CompletionResultType.Variable:
                    break;
            }

            return null;
        }
    }

    public double Priority => 0;

    public string? Text => CompletionText;
}