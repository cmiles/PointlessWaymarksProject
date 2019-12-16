using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using TheLemmonWorkshopWpfControls;
using TheLemmonWorkshopWpfControls.ContentList;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.ItemContentEditor;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopContentEditor
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private ContentListContext _contextListContext;
        private ControlStatusViewModel _statusContext;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            StatusContext = new ControlStatusViewModel();
            ContextListContext = new ContentListContext(StatusContext);

            NewContentCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(NewContent));
            ToastTestCommand = new RelayCommand(() => StatusContext.ToastWarning("Test"));

            UserSettingsUtilities.VerifyAndCreate();

            var db = Db.Context();
            db.Database.EnsureCreated();
        }

        public ContentListContext ContextListContext
        {
            get => _contextListContext;
            set
            {
                if (Equals(value, _contextListContext)) return;
                _contextListContext = value;
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


        public event PropertyChangedEventHandler PropertyChanged;

        public async Task CreateHtmlDoc()
        {
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task NewContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new ItemContentEditorWindow {Left = Left + 4, Top = Top + 4};
            newContentWindow.Show();
        }
    }
}