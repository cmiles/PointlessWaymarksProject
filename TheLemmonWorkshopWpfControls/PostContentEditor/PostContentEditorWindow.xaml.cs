using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;
using TheLemmonWorkshopData.Models;
using TheLemmonWorkshopWpfControls.ControlStatus;

namespace TheLemmonWorkshopWpfControls.PostContentEditor
{
    public partial class PostContentEditorWindow : INotifyPropertyChanged
    {
        private PostContentEditorContext _postContent;
        private StatusControlContext _statusContext;

        public PostContentEditorWindow(PostContent toLoad)
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            PostContent = new PostContentEditorContext(StatusContext, toLoad);

            DataContext = this;
        }

        public PostContentEditorContext PostContent
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