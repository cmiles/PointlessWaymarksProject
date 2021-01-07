using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Status;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.CmsWpfControls.Utility.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.NoteContentEditor
{
    /// <summary>
    ///     Interaction logic for NoteContentEditorWindow.xaml
    /// </summary>
    public partial class NoteContentEditorWindow : INotifyPropertyChanged
    {
        private NoteContentEditorContext _noteContent;
        private StatusControlContext _statusContext;

        public NoteContentEditorWindow(NoteContent toLoad)
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(async () =>
            {
                NoteContent = await NoteContentEditorContext.CreateInstance(StatusContext, toLoad);

                NoteContent.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
                AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, NoteContent);

                await ThreadSwitcher.ResumeForegroundAsync();
                DataContext = this;
            });
        }

        public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }

        public NoteContentEditorContext NoteContent
        {
            get => _noteContent;
            set
            {
                if (Equals(value, _noteContent)) return;
                _noteContent = value;
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