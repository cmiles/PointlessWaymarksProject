using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.ImageContentEditor
{
    public partial class ImageContentEditorWindow : INotifyPropertyChanged
    {
        private ImageContentEditorContext _imageEditor;
        private StatusControlContext _statusContext;

        public ImageContentEditorWindow()
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();
            ImageEditor = new ImageContentEditorContext(StatusContext);

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, ImageEditor);
        }

        public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }

        public ImageContentEditorWindow(FileInfo initialImage)
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();
            ImageEditor = new ImageContentEditorContext(StatusContext, initialImage);

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, ImageEditor);
        }

        public ImageContentEditorWindow(ImageContent toLoad)
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();
            ImageEditor = new ImageContentEditorContext(StatusContext, toLoad);

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, ImageEditor);
        }

        public ImageContentEditorContext ImageEditor
        {
            get => _imageEditor;
            set
            {
                if (Equals(value, _imageEditor)) return;
                _imageEditor = value;
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