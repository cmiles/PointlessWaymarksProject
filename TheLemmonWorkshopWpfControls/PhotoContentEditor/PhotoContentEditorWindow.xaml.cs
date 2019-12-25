using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
    }
}