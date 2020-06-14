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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}