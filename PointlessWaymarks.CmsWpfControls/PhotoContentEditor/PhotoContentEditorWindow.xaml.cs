using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PhotoContentEditor
{
    public partial class PhotoContentEditorWindow : INotifyPropertyChanged
    {
        private PhotoContentEditorContext _photoEditor;
        private StatusControlContext _statusContext;

        public PhotoContentEditorWindow()
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(async () =>
            {
                PhotoEditor = await PhotoContentEditorContext.CreateInstance(StatusContext);

                PhotoEditor.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
                AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PhotoEditor);

                await ThreadSwitcher.ResumeForegroundAsync();
                DataContext = this;
            });
        }

        public PhotoContentEditorWindow(FileInfo initialPhoto)
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(async () =>
            {
                PhotoEditor = await PhotoContentEditorContext.CreateInstance(StatusContext, initialPhoto);

                PhotoEditor.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
                AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PhotoEditor);

                await ThreadSwitcher.ResumeForegroundAsync();
                DataContext = this;
            });
        }

        public PhotoContentEditorWindow(PhotoContent toLoad)
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(async () =>
            {
                PhotoEditor = await PhotoContentEditorContext.CreateInstance(StatusContext, toLoad);

                PhotoEditor.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
                AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PhotoEditor);

                await ThreadSwitcher.ResumeForegroundAsync();
                DataContext = this;
            });
        }

        public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}