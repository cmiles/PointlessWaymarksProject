using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using TheLemmonWorkshopWpfControls.ControlStatus;

namespace TheLemmonWorkshopWpfControls.ItemContentEditor
{
    /// <summary>
    ///     Interaction logic for ItemContentEditorWindow.xaml
    /// </summary>
    public partial class ItemContentEditorWindow : INotifyPropertyChanged
    {
        private ItemContentEditorContext _itemContentEditorContext;
        private StatusControlContext _statusContext;

        public ItemContentEditorWindow()
        {
            InitializeComponent();
            StatusContext = new ControlStatus.StatusControlContext();

            DataContext = this;

            ItemContentEditorContext = new ItemContentEditorContext();
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

        public ItemContentEditorContext ItemContentEditorContext
        {
            get => _itemContentEditorContext;
            set
            {
                if (Equals(value, _itemContentEditorContext)) return;
                _itemContentEditorContext = value;
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