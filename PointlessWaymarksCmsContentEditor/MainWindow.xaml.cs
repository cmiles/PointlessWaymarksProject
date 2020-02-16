using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.ContentListHtml;
using PointlessWaymarksCmsData.FileHtml;
using PointlessWaymarksCmsData.ImageHtml;
using PointlessWaymarksCmsData.IndexHtml;
using PointlessWaymarksCmsData.NoteHtml;
using PointlessWaymarksCmsData.PhotoHtml;
using PointlessWaymarksCmsData.PostHtml;
using PointlessWaymarksCmsWpfControls.FileContentEditor;
using PointlessWaymarksCmsWpfControls.FileList;
using PointlessWaymarksCmsWpfControls.ImageContentEditor;
using PointlessWaymarksCmsWpfControls.ImageList;
using PointlessWaymarksCmsWpfControls.LinkStreamEditor;
using PointlessWaymarksCmsWpfControls.NoteList;
using PointlessWaymarksCmsWpfControls.PhotoContentEditor;
using PointlessWaymarksCmsWpfControls.PhotoList;
using PointlessWaymarksCmsWpfControls.PostContentEditor;
using PointlessWaymarksCmsWpfControls.PostList;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.UserSettingsEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsContentEditor
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private RelayCommand _generateAllHtmlCommand;
        private RelayCommand _generateHtmlForAllFileContentCommand;
        private RelayCommand _generateHtmlForAllImageContentCommand;
        private RelayCommand _generateHtmlForAllPhotoContentCommand;
        private RelayCommand _generateHtmlForAllPostContentCommand;
        private RelayCommand _generateIndexCommand;
        private RelayCommand _newFileContentCommand;
        private RelayCommand _newImageContentCommand;
        private RelayCommand _newLinkContentCommand;
        private RelayCommand _newPhotoContentCommand;
        private RelayCommand _newPostContentCommand;
        private RelayCommand _openIndexUrlCommand;
        private UserSettingsEditorContext _settingsEditorContext;
        private StatusControlContext _statusContext;
        private FileListWithActionsContext _tabFileListContext;
        private ImageListWithActionsContext _tabImageListContext;
        private NoteListWithActionsContext _tabNoteListContext;
        private PhotoListWithActionsContext _tabPhotoListContext;
        private PostListWithActionsContext _tabPostListContext;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            StatusContext = new StatusControlContext();

            GenerateIndexCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(GenerateIndex));
            OpenIndexUrlCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(OpenIndexUrl));
            GenerateAllHtmlCommand = new RelayCommand(() => StatusContext.RunBlockingTask(GenerateAllHtml));

            NewPhotoContentCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(NewPhotoContent));
            GenerateHtmlForAllPhotoContentCommand =
                new RelayCommand(() => StatusContext.RunBlockingTask(GenerateAllPhotoHtml));

            NewPostContentCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(NewPostContent));
            GenerateHtmlForAllPostContentCommand =
                new RelayCommand(() => StatusContext.RunBlockingTask(GenerateAllPostHtml));

            NewImageContentCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(NewImageContent));
            GenerateHtmlForAllImageContentCommand =
                new RelayCommand(() => StatusContext.RunBlockingTask(GenerateAllImageHtml));

            NewFileContentCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(NewFileContent));
            GenerateHtmlForAllFileContentCommand =
                new RelayCommand(() => StatusContext.RunBlockingTask(GenerateAllFileHtml));

            NewLinkContentCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(NewLinkContent));

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(LoadData);
        }

        public RelayCommand GenerateAllHtmlCommand
        {
            get => _generateAllHtmlCommand;
            set
            {
                if (Equals(value, _generateAllHtmlCommand)) return;
                _generateAllHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand GenerateHtmlForAllFileContentCommand
        {
            get => _generateHtmlForAllFileContentCommand;
            set
            {
                if (Equals(value, _generateHtmlForAllFileContentCommand)) return;
                _generateHtmlForAllFileContentCommand = value;
                OnPropertyChanged();
            }
        }


        public RelayCommand GenerateHtmlForAllImageContentCommand
        {
            get => _generateHtmlForAllImageContentCommand;
            set
            {
                if (Equals(value, _generateHtmlForAllImageContentCommand)) return;
                _generateHtmlForAllImageContentCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand GenerateHtmlForAllPhotoContentCommand
        {
            get => _generateHtmlForAllPhotoContentCommand;
            set
            {
                if (Equals(value, _generateHtmlForAllPhotoContentCommand)) return;
                _generateHtmlForAllPhotoContentCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand GenerateHtmlForAllPostContentCommand
        {
            get => _generateHtmlForAllPostContentCommand;
            set
            {
                if (Equals(value, _generateHtmlForAllPostContentCommand)) return;
                _generateHtmlForAllPostContentCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand GenerateIndexCommand
        {
            get => _generateIndexCommand;
            set
            {
                if (Equals(value, _generateIndexCommand)) return;
                _generateIndexCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand NewFileContentCommand
        {
            get => _newFileContentCommand;
            set
            {
                if (Equals(value, _newFileContentCommand)) return;
                _newFileContentCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand NewImageContentCommand
        {
            get => _newImageContentCommand;
            set
            {
                if (Equals(value, _newImageContentCommand)) return;
                _newImageContentCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand NewLinkContentCommand
        {
            get => _newLinkContentCommand;
            set
            {
                if (Equals(value, _newLinkContentCommand)) return;
                _newLinkContentCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand NewPhotoContentCommand
        {
            get => _newPhotoContentCommand;
            set
            {
                if (Equals(value, _newPhotoContentCommand)) return;
                _newPhotoContentCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand NewPostContentCommand
        {
            get => _newPostContentCommand;
            set
            {
                if (Equals(value, _newPostContentCommand)) return;
                _newPostContentCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand OpenIndexUrlCommand
        {
            get => _openIndexUrlCommand;
            set
            {
                if (Equals(value, _openIndexUrlCommand)) return;
                _openIndexUrlCommand = value;
                OnPropertyChanged();
            }
        }

        public UserSettingsEditorContext SettingsEditorContext
        {
            get => _settingsEditorContext;
            set
            {
                if (Equals(value, _settingsEditorContext)) return;
                _settingsEditorContext = value;
                OnPropertyChanged();
            }
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

        public FileListWithActionsContext TabFileListContext
        {
            get => _tabFileListContext;
            set
            {
                if (Equals(value, _tabFileListContext)) return;
                _tabFileListContext = value;
                OnPropertyChanged();
            }
        }

        public ImageListWithActionsContext TabImageListContext
        {
            get => _tabImageListContext;
            set
            {
                if (Equals(value, _tabImageListContext)) return;
                _tabImageListContext = value;
                OnPropertyChanged();
            }
        }

        public NoteListWithActionsContext TabNoteListContext
        {
            get => _tabNoteListContext;
            set
            {
                if (Equals(value, _tabNoteListContext)) return;
                _tabNoteListContext = value;
                OnPropertyChanged();
            }
        }

        public PhotoListWithActionsContext TabPhotoListContext
        {
            get => _tabPhotoListContext;
            set
            {
                if (Equals(value, _tabPhotoListContext)) return;
                _tabPhotoListContext = value;
                OnPropertyChanged();
            }
        }

        public PostListWithActionsContext TabPostListContext
        {
            get => _tabPostListContext;
            set
            {
                if (Equals(value, _tabPostListContext)) return;
                _tabPostListContext = value;
                OnPropertyChanged();
            }
        }

        private async Task GenerateAllFileHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            var allItems = await db.FileContents.ToListAsync();

            var loopCount = 0;
            var totalCount = allItems.Count;

            StatusContext.Progress($"Found {totalCount} Files to Generate");

            foreach (var loopItem in allItems)
            {
                StatusContext.Progress($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SingleFilePage(loopItem);
                htmlModel.WriteLocalHtml();

                loopCount++;
            }
        }

        private async Task GenerateAllHtml()
        {
            await GenerateAllFileHtml();
            await GenerateAllImageHtml();
            await GenerateAllPhotoHtml();
            await GenerateAllPostHtml();
            await GenerateAllNoteHtml();
            await GenerateAllListHtml();
            await GenerateIndex();
        }

        private async Task GenerateAllImageHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            var allItems = await db.ImageContents.ToListAsync();

            var loopCount = 0;
            var totalCount = allItems.Count;

            StatusContext.Progress($"Found {totalCount} Images to Generate");

            foreach (var loopItem in allItems)
            {
                StatusContext.Progress($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SingleImagePage(loopItem);
                htmlModel.WriteLocalHtml();

                loopCount++;
            }
        }

        private async Task GenerateAllListHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ContentListPageGenerators.WriteAllContentListHtml();
            ContentListPageGenerators.WriteFileContentListHtml();
            ContentListPageGenerators.WriteImageContentListHtml();
            ContentListPageGenerators.WritePhotoContentListHtml();
            ContentListPageGenerators.WritePostContentListHtml();
            ContentListPageGenerators.WriteNoteContentListHtml();
        }

        private async Task GenerateAllNoteHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            var allItems = await db.NoteContents.ToListAsync();

            var loopCount = 0;
            var totalCount = allItems.Count;

            StatusContext.Progress($"Found {totalCount} Posts to Generate");

            foreach (var loopItem in allItems)
            {
                StatusContext.Progress(
                    $"Writing HTML for Note Dated {loopItem.CreatedOn:d} - {loopCount} of {totalCount}");

                var htmlModel = new SingleNotePage(loopItem);
                htmlModel.WriteLocalHtml();

                loopCount++;
            }
        }

        private async Task GenerateAllPhotoHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            var allItems = await db.PhotoContents.ToListAsync();

            var loopCount = 0;
            var totalCount = allItems.Count;

            StatusContext.Progress($"Found {totalCount} Photos to Generate");

            foreach (var loopItem in allItems)
            {
                StatusContext.Progress($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SinglePhotoPage(loopItem);
                htmlModel.WriteLocalHtml();

                loopCount++;
            }
        }

        private async Task GenerateAllPostHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            var allItems = await db.PostContents.ToListAsync();

            var loopCount = 0;
            var totalCount = allItems.Count;

            StatusContext.Progress($"Found {totalCount} Posts to Generate");

            foreach (var loopItem in allItems)
            {
                StatusContext.Progress($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SinglePostPage(loopItem);
                htmlModel.WriteLocalHtml();

                loopCount++;
            }
        }

        private async Task GenerateIndex()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var index = new IndexPage();
            index.WriteLocalHtml();

            StatusContext.ToastSuccess($"Generated {index.PageUrl}");
        }

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            TabImageListContext = new ImageListWithActionsContext(null);
            TabFileListContext = new FileListWithActionsContext(null);
            TabPhotoListContext = new PhotoListWithActionsContext(null);
            TabPostListContext = new PostListWithActionsContext(null);
            TabNoteListContext = new NoteListWithActionsContext(null);
            SettingsEditorContext =
                new UserSettingsEditorContext(StatusContext, UserSettingsSingleton.CurrentSettings());

            UserSettingsUtilities.VerifyAndCreate();

            var db = await Db.Context();
            //db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }

        private async Task NewFileContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new FileContentEditorWindow(null) {Left = Left + 4, Top = Top + 4};
            newContentWindow.Show();
        }

        private async Task NewImageContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new ImageContentEditorWindow(null) {Left = Left + 4, Top = Top + 4};
            newContentWindow.Show();
        }

        private async Task NewLinkContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new LinkStreamEditorWindow(null) {Left = Left + 4, Top = Top + 4};
            newContentWindow.Show();
        }

        private async Task NewPhotoContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new PhotoContentEditorWindow(null) {Left = Left + 4, Top = Top + 4};
            newContentWindow.Show();
        }

        private async Task NewPostContent()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new PostContentEditorWindow(null) {Left = Left + 4, Top = Top + 4};
            newContentWindow.Show();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OpenIndexUrl()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var settings = UserSettingsSingleton.CurrentSettings();

            var url = $@"http://{settings.SiteUrl}";

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}