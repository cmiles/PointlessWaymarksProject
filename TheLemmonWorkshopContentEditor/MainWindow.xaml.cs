using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TheLemmonWorkshopWpfControls;
using TheLemmonWorkshopWpfControls.ContentList;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.ItemContentEditor;
using TheLemmonWorkshopWpfControls.PhotoContentEditor;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopContentEditor
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private ContentListContext _contextListContext;
        private StatusControlContext _statusContext;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            StatusContext = new StatusControlContext();

            UserSettingsUtilities.VerifyAndCreate();

            var db = Db.Context();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            ContextListContext = new ContentListContext(StatusContext);
            NewContentCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(NewContent));
            NewPhotoContentCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(NewPhotoContent));
            ToastTestCommand = new RelayCommand(() => StatusContext.ToastWarning("Test"));
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
        public RelayCommand NewPhotoContentCommand { get; set; }

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

        public RelayCommand ToastTestCommand { get; set; }

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

        private async Task NewPhotoContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new PhotoContentEditorWindow(null) {Left = Left + 4, Top = Top + 4};
            newContentWindow.Show();
        }
    }
}