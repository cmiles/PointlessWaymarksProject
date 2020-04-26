using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.LinkStreamEditor
{
    public partial class LinkStreamEditorWindow : INotifyPropertyChanged
    {
        private LinkStreamEditorContext _editorContent;
        private StatusControlContext _statusContext;

        public LinkStreamEditorWindow(LinkStream toLoad, bool extractDataFromLink = false)
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            EditorContent = new LinkStreamEditorContext(StatusContext, toLoad, extractDataFromLink);

            EditorContent.RequestLinkStreamEditorWindowClose += (sender, args) => { Dispatcher?.Invoke(Close); };

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, EditorContent);
        }

        public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }

        public LinkStreamEditorContext EditorContent
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