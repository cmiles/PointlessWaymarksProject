using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TheLemmonWorkshopData.Models;
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

            NewContentCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(NewContent));
            NewPhotoContentCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(NewPhotoContent));
EditPhotoContentCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(EditPhotoContent));
            
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(LoadData);
        }

        private async Task EditPhotoContent()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (ContextListContext.SelectedItem == null)
            {
                StatusContext.ToastWarning("Nothing Selected?");
                return;
            }
            
            if(ContextListContext.SelectedItem.ContentType != "Photo")
            {
                StatusContext.ToastWarning("Photo not Selected.");
                return;
            }

            var photo = (PhotoContent)ContextListContext.SelectedItem.SummaryInfo;

            await ThreadSwitcher.ResumeForegroundAsync();
            
            var newContentWindow = new PhotoContentEditorWindow(photo) {Left = Left + 4, Top = Top + 4};
            newContentWindow.Show();
        }

        public RelayCommand EditPhotoContentCommand { get; set; }

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            UserSettingsUtilities.VerifyAndCreate();

            var db = await Db.Context();
            //db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            ContextListContext = new ContentListContext(StatusContext);
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