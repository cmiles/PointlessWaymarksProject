using System.ComponentModel;
using System.Windows;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.WpfCommon.ChangesAndValidation;

public class WindowAccidentalClosureHelper
{
    private readonly IHasChanges _hasChangesToCheck;
    private readonly Window _toClose;
    private bool _closeConfirmed;

    public WindowAccidentalClosureHelper(Window toClose, StatusControlContext? context, IHasChanges toCheck)
    {
        StatusContext = context;
        _hasChangesToCheck = toCheck;
        _toClose = toClose;

        _toClose.Closing += FileContentEditorWindow_OnClosing;
    }

    public Action<Window>? CloseAction { get; init; }

    public StatusControlContext? StatusContext { get; }

    private void FileContentEditorWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (_closeConfirmed) return;

        e.Cancel = true;

        StatusContext.RunFireAndForgetBlockingTask(WindowClosing);
    }

    private async Task WindowClosing()
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeBackgroundAsync();

        if (!_hasChangesToCheck.HasChanges)
        {
            _closeConfirmed = true;
            CloseAction?.Invoke(_toClose);
            await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();
            _toClose.Close();
        }

        if (await StatusContext.ShowMessage("Unsaved Changes...",
                "There are unsaved changes - do you want to discard your changes?",
                new List<string> { "Yes - Close Window", "No" }) == "Yes - Close Window")
        {
            _closeConfirmed = true;
            CloseAction?.Invoke(_toClose);
            await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();
            _toClose.Close();
        }
    }
}