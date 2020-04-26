using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.NoteContentEditor
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
            NoteContent = new NoteContentEditorContext(StatusContext, toLoad);

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, NoteContent);
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}