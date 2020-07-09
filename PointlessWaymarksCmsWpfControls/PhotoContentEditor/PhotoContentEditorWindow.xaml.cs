using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.PhotoContentEditor
{
    public partial class PhotoContentEditorWindow : INotifyPropertyChanged
    {
        private PhotoContentEditorContext _photoEditor;
        private StatusControlContext _statusContext;

        public PhotoContentEditorWindow()
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();
            PhotoEditor = new PhotoContentEditorContext(StatusContext, false);

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PhotoEditor);
        }

        public PhotoContentEditorWindow(bool forAutomation)
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();
            PhotoEditor = new PhotoContentEditorContext(StatusContext, forAutomation);

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PhotoEditor);
        }

        public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }

        public PhotoContentEditorWindow(FileInfo initialPhoto)
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();
            PhotoEditor = new PhotoContentEditorContext(StatusContext, initialPhoto);

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PhotoEditor);
        }

        public PhotoContentEditorWindow(PhotoContent toLoad)
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();
            PhotoEditor = new PhotoContentEditorContext(StatusContext, toLoad);

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PhotoEditor);
        }

        public PhotoContentEditorContext PhotoEditor
        {
            get => _photoEditor;
            set
            {
                if (Equals(value, _photoEditor)) return;
                _photoEditor = value;
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