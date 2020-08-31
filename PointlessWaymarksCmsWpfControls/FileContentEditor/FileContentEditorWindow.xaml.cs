using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.FileContentEditor
{
    public partial class FileContentEditorWindow : INotifyPropertyChanged
    {
        private FileContentEditorContext _postContent;
        private StatusControlContext _statusContext;

        public FileContentEditorWindow()
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            FileContent = new FileContentEditorContext(StatusContext);

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, FileContent);
        }

        public FileContentEditorWindow(FileInfo initialFile)
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            FileContent = new FileContentEditorContext(StatusContext, initialFile);

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, FileContent);
        }

        public FileContentEditorWindow(FileContent toLoad)
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            FileContent = new FileContentEditorContext(StatusContext, toLoad);

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, FileContent);
        }

        public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }

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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}