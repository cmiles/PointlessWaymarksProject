using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.LinkContentEditor
{
    public partial class LinkContentEditorWindow : INotifyPropertyChanged
    {
        private LinkContentEditorContext _editorContent;
        private StatusControlContext _statusContext;

        public LinkContentEditorWindow(LinkContent toLoad, bool extractDataFromLink = false)
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            EditorContent = new LinkContentEditorContext(StatusContext, toLoad, extractDataFromLink);

            EditorContent.RequestLinkContentEditorWindowClose += (sender, args) => { Dispatcher?.Invoke(Close); };

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, EditorContent);
        }

        public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }

        public LinkContentEditorContext EditorContent
        {
            get => _editorContent;
            set
            {
                if (Equals(value, _editorContent)) return;
                _editorContent = value;
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