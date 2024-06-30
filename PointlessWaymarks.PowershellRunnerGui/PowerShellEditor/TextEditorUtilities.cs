using System.Management.Automation;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace PointlessWaymarks.PowerShellRunnerGui.PowerShellEditor;

//Code copied from or based on [GitHub - dfinke/PowerShellConsole: Create a PowerShell Console using the AvalonEdit control](https://github.com/dfinke/PowerShellConsole/tree/master)
//[dfinke (Doug Finke)](https://github.com/dfinke) -  Apache-2.0 license 
public class TextEditorUtilities
{
    private static CompletionWindow? _completionWindow;

    public static void InvokeCompletionWindow(TextEditor textEditor)
    {
        _completionWindow = new CompletionWindow(textEditor.TextArea);

        _completionWindow.Closed += delegate { _completionWindow = null; };

        var text = textEditor.Text;
        var offset = textEditor.TextArea.Caret.Offset;

        var completedInput =
            CommandCompletion.CompleteInput(text, offset, null, PsConsolePowerShell.PowerShellInstance);

        if (completedInput.CompletionMatches.Count > 0)
        {
            completedInput.CompletionMatches.ToList()
                .ForEach(record =>
                {
                    _completionWindow.CompletionList.CompletionData.Add(
                        new CompletionData
                        {
                            CompletionText = record.CompletionText,
                            ToolTip = record.ToolTip,
                            ResulType = record.ResultType,
                            ReplacementLength = completedInput.ReplacementLength
                        });
                });

            _completionWindow.Show();
        }
    }
}