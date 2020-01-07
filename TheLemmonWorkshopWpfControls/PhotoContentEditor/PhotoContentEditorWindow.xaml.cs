using JetBrains.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TheLemmonWorkshopData.Models;
using TheLemmonWorkshopWpfControls.ControlStatus;

namespace TheLemmonWorkshopWpfControls.PhotoContentEditor
{
    public partial class PhotoContentEditorWindow : INotifyPropertyChanged
    {
        private PhotoContentEditorContext _photoContentEditor;
        private StatusControlContext _statusContext;

        public PhotoContentEditorWindow(PhotoContent toLoad)
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();
            PhotoContentEditor = new PhotoContentEditorContext(StatusContext, toLoad);

            DataContext = this;
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

        public PhotoContentEditorContext PhotoContentEditor
        {
            get => _photoContentEditor;
            set
            {
                if (Equals(value, _photoContentEditor)) return;
                _photoContentEditor = value;
                OnPropertyChanged();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}