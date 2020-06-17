using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarksCmsWpfControls.PhotoList
{
    /// <summary>
    ///     Interaction logic for PhotoListWindow.xaml
    /// </summary>
    public partial class PhotoListWindow : INotifyPropertyChanged
    {
        private PhotoListWithActionsContext _photoListContext;
        private string _windowTitle = "Photo List";

        public PhotoListWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        public PhotoListWithActionsContext PhotoListContext
        {
            get => _photoListContext;
            set
            {
                if (Equals(value, _photoListContext)) return;
                _photoListContext = value;
                OnPropertyChanged();
            }
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (value == _windowTitle) return;
                _windowTitle = value;
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