using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TheLemmonWorkshopWpfControls;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.ItemContentEditor;
using TheLemmonWorkshopWpfControls.Settings;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopContentEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private ItemContentEditorViewModel _itemContentEditorContext;
        private ControlStatusViewModel _statusContext;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            StatusContext = new ControlStatusViewModel();

            NewContentCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(NewContent));
            ToastTestCommand = new RelayCommand(() => StatusContext.ToastWarning("Test"));

            SettingsUtility.VerifyAndCreate();

            var db = Db.Context();
            db.Database.EnsureCreated();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ItemContentEditorViewModel ItemContentEditorContext
        {
            get => _itemContentEditorContext;
            set
            {
                if (Equals(value, _itemContentEditorContext)) return;
                _itemContentEditorContext = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand NewContentCommand { get; set; }

        public ControlStatusViewModel StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ToastTestCommand { get; set; }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task NewContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new ItemContentEditorWindow { Left = Left + 4, Top = Top + 4 };
            newContentWindow.Show();
        }
    }
}