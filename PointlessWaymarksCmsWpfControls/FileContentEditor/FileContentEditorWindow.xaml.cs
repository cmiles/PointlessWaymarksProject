using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarksCmsWpfControls.FileContentEditor
{
    public partial class FileContentEditorWindow : INotifyPropertyChanged
    {
        public FileContentEditorWindow()
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