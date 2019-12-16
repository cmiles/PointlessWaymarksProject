using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace TheLemmonWorkshopWpfControls.ItemContentEditor
{
    /// <summary>
    ///     Interaction logic for ItemContentEditorWindow.xaml
    /// </summary>
    public partial class ItemContentEditorWindow : INotifyPropertyChanged
    {
        private ItemContentEditorContext _itemContentEditorContext;

        public ItemContentEditorWindow()
        {
            InitializeComponent();

            DataContext = this;

            ItemContentEditorContext = new ItemContentEditorContext();
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