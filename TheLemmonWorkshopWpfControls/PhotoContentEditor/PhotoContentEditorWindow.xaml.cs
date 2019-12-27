using JetBrains.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TheLemmonWorkshopData.Models;

namespace TheLemmonWorkshopWpfControls.PhotoContentEditor
{
    public partial class PhotoContentEditorWindow : INotifyPropertyChanged
    {
        private PhotoContentEditorContext _photoContentEditor;

        public PhotoContentEditorWindow(PhotoContent toLoad)
        {
            InitializeComponent();
            PhotoContentEditor = new PhotoContentEditorContext(null, toLoad);
            DataContext = this;
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