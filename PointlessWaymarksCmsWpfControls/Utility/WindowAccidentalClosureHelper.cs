using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public class WindowAccidentalClosureHelper
    {
        private bool _closeConfirmed;
        private readonly IHasUnsavedChanges _hasUnsavedChangesToCheck;
        private readonly Window _toClose;

        public WindowAccidentalClosureHelper(Window toClose, StatusControlContext context, IHasUnsavedChanges toCheck)
        {
            StatusContext = context;
            _hasUnsavedChangesToCheck = toCheck;
            _toClose = toClose;

            _toClose.Closing += FileContentEditorWindow_OnClosing;
        }

        public StatusControlContext StatusContext { get; set; }

        private void FileContentEditorWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (_closeConfirmed) return;

            e.Cancel = true;

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(WindowClosing);
        }

        private async Task WindowClosing()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (!_hasUnsavedChangesToCheck.HasChanges())
            {
                _closeConfirmed = true;
                await ThreadSwitcher.ResumeForegroundAsync();
                _toClose.Close();
            }

            if (await StatusContext.ShowMessage("Unsaved Changes...",
                "There are unsaved changes - do you want to discard your changes?",
                new List<string> {"Yes - Close Window", "No"}) == "Yes - Close Window")
            {
                _closeConfirmed = true;
                await ThreadSwitcher.ResumeForegroundAsync();
                _toClose.Close();
            }
        }
    }
}