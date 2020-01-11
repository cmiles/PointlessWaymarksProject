using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarksCmsWpfControls.ImageContentEditor
{
    public partial class ImageContentEditorWindow : INotifyPropertyChanged
    {
        public ImageContentEditorWindow()
        {
            InitializeComponent();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}