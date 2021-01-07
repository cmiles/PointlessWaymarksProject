using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Status;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.CmsWpfControls.Utility.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.ImageContentEditor
{
    public partial class ImageContentEditorWindow : INotifyPropertyChanged
    {
        private ImageContentEditorContext _imageEditor;
        private StatusControlContext _statusContext;

        public ImageContentEditorWindow()
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(async () =>
            {
                ImageEditor = await ImageContentEditorContext.CreateInstance(StatusContext);

                ImageEditor.RequestContentEditorWindowClose += (sender, args) => { Dispatcher?.Invoke(Close); };
                AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, ImageEditor);

                await ThreadSwitcher.ResumeForegroundAsync();
                DataContext = this;
            });
        }

        public ImageContentEditorWindow(ImageContent contentToLoad = null, FileInfo initialImage = null)
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(async () =>
            {
                ImageEditor =
                    await ImageContentEditorContext.CreateInstance(StatusContext, contentToLoad, initialImage);

                ImageEditor.RequestContentEditorWindowClose += (sender, args) => { Dispatcher?.Invoke(Close); };
                AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, ImageEditor);

                await ThreadSwitcher.ResumeForegroundAsync();
                DataContext = this;
            });
        }

        public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}