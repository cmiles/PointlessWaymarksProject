using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.FileContentEditor
{
    public partial class FileContentEditorWindow : INotifyPropertyChanged
    {
        private FileContentEditorContext _postContent;
        private StatusControlContext _statusContext;

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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}