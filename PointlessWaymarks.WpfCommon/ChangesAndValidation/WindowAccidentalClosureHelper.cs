using System.ComponentModel;
using System.Windows;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.WpfCommon.ChangesAndValidation;

public class WindowAccidentalClosureHelper
{
    private readonly IHasChanges _hasChangesToCheck;
    private readonly Window _toClose;
    private bool _closeConfirmed;
    private bool _windowCloseDialogRunning;

    public WindowAccidentalClosureHelper(Window toClose, StatusControlContext context, IHasChanges toCheck)
    {
        StatusContext = context;
        _hasChangesToCheck = toCheck;
        _toClose = toClose;

        _toClose.Closing += Window_OnClosing;
    }

    public Action<Window>? CloseAction { get; init; }
    public StatusControlContext StatusContext { get; }

    private void Window_OnClosing(object? sender, CancelEventArgs e)
    {
        if (_closeConfirmed) return;

        e.Cancel = true;

        //2024/4/8 - this naive guard is to cover the user clicking the close
        //button while the Unsaved Changes dialog is running - if WindowClosing
        //is allowed to run twice it won't work correctly an answer that returns
        //to the editor ('No') will result in the control covered by the Status
        //Control without any interaction possible.
        if (_windowCloseDialogRunning) return;

        StatusContext.RunFireAndForgetBlockingTask(WindowClosing);
    }

    private async Task WindowClosing()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!_hasChangesToCheck.HasChanges)
        {
            _closeConfirmed = true;
            CloseAction?.Invoke(_toClose);
            await ThreadSwitcher.ResumeForegroundAsync();
            _toClose.Close();
        }

        _windowCloseDialogRunning = true;

        if (await StatusContext.ShowMessage("Unsaved Changes...",
                "There are unsaved changes - do you want to discard your changes?",
                ["Yes - Close Window", "No"]) == "Yes - Close Window")
        {
            _closeConfirmed = true;
            CloseAction?.Invoke(_toClose);
            await ThreadSwitcher.ResumeForegroundAsync();
            _toClose.Close();
        }

        _windowCloseDialogRunning = false;
    }
}