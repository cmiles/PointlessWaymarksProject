using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.FileContentEditor
{
    public partial class FileContentEditorWindow : INotifyPropertyChanged
    {
        private bool _closeConfirmed;
        private FileContentEditorContext _postContent;
        private StatusControlContext _statusContext;

        public FileContentEditorWindow()
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            FileContent = new FileContentEditorContext(StatusContext);

            DataContext = this;
        }

        public FileContentEditorWindow(FileInfo initialFile)
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            FileContent = new FileContentEditorContext(StatusContext, initialFile);

            DataContext = this;
        }

        public FileContentEditorWindow(FileContent toLoad)
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            FileContent = new FileContentEditorContext(StatusContext, toLoad);

            DataContext = this;
        }

        public FileContentEditorContext FileContent
        {
            get => _postContent;
            set
            {
                if (Equals(value, _postContent)) return;
                _postContent = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void FileContentEditorWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (_closeConfirmed) return;

            e.Cancel = true;

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(WindowClosing);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task WindowClosing()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (!FileContent.HasUnsavedChanges())
            {
                _closeConfirmed = true;
                await ThreadSwitcher.ResumeForegroundAsync();
                Close();
            }

            ;

            if (await StatusContext.ShowMessage("Unsaved Changes...",
                "There are unsaved changes - do you want to discard your changes?",
                new List<string> {"Yes - Close Window", "No"}) == "Yes - Close Window")
            {
                _closeConfirmed = true;
                await ThreadSwitcher.ResumeForegroundAsync();
                Close();
            }
        }
    }
}