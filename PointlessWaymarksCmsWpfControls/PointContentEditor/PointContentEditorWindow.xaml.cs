using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.PointContentEditor
{
    /// <summary>
    ///     Interaction logic for PointContentEditorWindow.xaml
    /// </summary>
    public partial class PointContentEditorWindow : INotifyPropertyChanged
    {
        private PointContentEditorContext _postContent;
        private StatusControlContext _statusContext;

        public PointContentEditorWindow(PointContent toLoad)
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            PointContent = new PointContentEditorContext(StatusContext, toLoad);

            DataContext = this;
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PointContent);
        }

        public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }

        public PointContentEditorContext PointContent
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