using System.Windows.Input;
using ICSharpCode.AvalonEdit;

namespace PointlessWaymarks.PowerShellRunnerGui.PowerShellEditor;

//Code copied from or based on [GitHub - dfinke/PowerShellConsole: Create a PowerShell Console using the AvalonEdit control](https://github.com/dfinke/PowerShellConsole/tree/master)
//[dfinke (Doug Finke)](https://github.com/dfinke) -  Apache-2.0 license 
public class ControlSpaceBarCommand(TextEditor textEditor) : ICommand
{
    private readonly TextEditor _textEditor = textEditor ??
                                              throw new ArgumentNullException(
                                                  $"{nameof(textEditor)} in ControlSpaceBarCommand ctor");

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public event EventHandler? CanExecuteChanged;

    public void Execute(object? parameter)
    {
        TextEditorUtilities.InvokeCompletionWindow(_textEditor);
    }
}