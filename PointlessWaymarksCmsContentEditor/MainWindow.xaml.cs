using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using Ookii.Dialogs.Wpf;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.ContentListHtml;
using PointlessWaymarksCmsData.FileHtml;
using PointlessWaymarksCmsData.ImageHtml;
using PointlessWaymarksCmsData.IndexHtml;
using PointlessWaymarksCmsData.JsonFiles;
using PointlessWaymarksCmsData.LinkListHtml;
using PointlessWaymarksCmsData.NoteHtml;
using PointlessWaymarksCmsData.PhotoHtml;
using PointlessWaymarksCmsData.Pictures;
using PointlessWaymarksCmsData.PostHtml;
using PointlessWaymarksCmsWpfControls.FileContentEditor;
using PointlessWaymarksCmsWpfControls.FileList;
using PointlessWaymarksCmsWpfControls.ImageContentEditor;
using PointlessWaymarksCmsWpfControls.ImageList;
using PointlessWaymarksCmsWpfControls.LinkStreamEditor;
using PointlessWaymarksCmsWpfControls.LinkStreamList;
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
        private Command _generateAllHtmlCommand;
        private Command _generateHtmlForAllFileContentCommand;
        private Command _generateHtmlForAllImageContentCommand;
        private Command _generateHtmlForAllPhotoContentCommand;
        private Command _generateHtmlForAllPostContentCommand;
        private Command _generateIndexCommand;
        private LinkStreamListWithActionsContext _linkStreamContext;
        private Command _newFileContentCommand;
        private Command _newImageContentCommand;
        private Command _newLinkContentCommand;
        private Command _newPhotoContentCommand;
        private Command _newPostContentCommand;
        private Command _openIndexUrlCommand;
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

            GenerateIndexCommand = new Command(() => StatusContext.RunNonBlockingTask(GenerateIndex));
            OpenIndexUrlCommand = new Command(() => StatusContext.RunNonBlockingTask(OpenIndexUrl));

            GenerateAllHtmlCommand = new Command(() => StatusContext.RunBlockingTask(GenerateAllHtml));
            GenerateAllHtmlAndCleanAndResizePicturesCommand = new Command(() =>
                StatusContext.RunBlockingTask(GenerateAllHtmlAndCleanAndResizePictures));
            CleanAndResizePicturesCommand = new Command(() => StatusContext.RunBlockingTask(CleanAndResizePictures));

            NewPhotoContentCommand = new Command(() => StatusContext.RunNonBlockingTask(NewPhotoContent));
            GenerateHtmlForAllPhotoContentCommand =
                new Command(() => StatusContext.RunBlockingTask(GenerateAllPhotoHtml));

            NewPostContentCommand = new Command(() => StatusContext.RunNonBlockingTask(NewPostContent));
            GenerateHtmlForAllPostContentCommand =
                new Command(() => StatusContext.RunBlockingTask(GenerateAllPostHtml));

            NewImageContentCommand = new Command(() => StatusContext.RunNonBlockingTask(NewImageContent));
            GenerateHtmlForAllImageContentCommand =
                new Command(() => StatusContext.RunBlockingTask(GenerateAllImageHtml));

            NewFileContentCommand = new Command(() => StatusContext.RunNonBlockingTask(NewFileContent));
            GenerateHtmlForAllFileContentCommand =
                new Command(() => StatusContext.RunBlockingTask(GenerateAllFileHtml));

            NewLinkContentCommand = new Command(() => StatusContext.RunNonBlockingTask(NewLinkContent));

            ImportJsonFromDirectoryCommand =
                new Command(() => StatusContext.RunNonBlockingTask(ImportJsonFromDirectory));

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(LoadData);
        }

        public Command CleanAndResizePicturesCommand { get; set; }

        public Command GenerateAllHtmlAndCleanAndResizePicturesCommand { get; set; }

        public Command GenerateAllHtmlCommand
        {
            get => _generateAllHtmlCommand;
            set
            {
                if (Equals(value, _generateAllHtmlCommand)) return;
                _generateAllHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public Command GenerateHtmlForAllFileContentCommand
        {
            get => _generateHtmlForAllFileContentCommand;
            set
            {
                if (Equals(value, _generateHtmlForAllFileContentCommand)) return;
                _generateHtmlForAllFileContentCommand = value;
                OnPropertyChanged();
            }
        }


        public Command GenerateHtmlForAllImageContentCommand
        {
            get => _generateHtmlForAllImageContentCommand;
            set
            {
                if (Equals(value, _generateHtmlForAllImageContentCommand)) return;
                _generateHtmlForAllImageContentCommand = value;
                OnPropertyChanged();
            }
        }

        public Command GenerateHtmlForAllPhotoContentCommand
        {
            get => _generateHtmlForAllPhotoContentCommand;
            set
            {
                if (Equals(value, _generateHtmlForAllPhotoContentCommand)) return;
                _generateHtmlForAllPhotoContentCommand = value;
                OnPropertyChanged();
            }
        }

        public Command GenerateHtmlForAllPostContentCommand
        {
            get => _generateHtmlForAllPostContentCommand;
            set
            {
                if (Equals(value, _generateHtmlForAllPostContentCommand)) return;
                _generateHtmlForAllPostContentCommand = value;
                OnPropertyChanged();
            }
        }

        public Command GenerateIndexCommand
        {
            get => _generateIndexCommand;
            set
            {
                if (Equals(value, _generateIndexCommand)) return;
                _generateIndexCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ImportJsonFromDirectoryCommand { get; set; }

        public LinkStreamListWithActionsContext LinkStreamContext
        {
            get => _linkStreamContext;
            set
            {
                if (Equals(value, _linkStreamContext)) return;
                _linkStreamContext = value;
                OnPropertyChanged();
            }
        }

        public Command NewFileContentCommand
        {
            get => _newFileContentCommand;
            set
            {
                if (Equals(value, _newFileContentCommand)) return;
                _newFileContentCommand = value;
                OnPropertyChanged();
            }
        }

        public Command NewImageContentCommand
        {
            get => _newImageContentCommand;
            set
            {
                if (Equals(value, _newImageContentCommand)) return;
                _newImageContentCommand = value;
                OnPropertyChanged();
            }
        }

        public Command NewLinkContentCommand
        {
            get => _newLinkContentCommand;
            set
            {
                if (Equals(value, _newLinkContentCommand)) return;
                _newLinkContentCommand = value;
                OnPropertyChanged();
            }
        }

        public Command NewPhotoContentCommand
        {
            get => _newPhotoContentCommand;
            set
            {
                if (Equals(value, _newPhotoContentCommand)) return;
                _newPhotoContentCommand = value;
                OnPropertyChanged();
            }
        }

        public Command NewPostContentCommand
        {
            get => _newPostContentCommand;
            set
            {
                if (Equals(value, _newPostContentCommand)) return;
                _newPostContentCommand = value;
                OnPropertyChanged();
            }
        }

        public Command OpenIndexUrlCommand
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

        private async Task CleanAndResizeAllImageFiles()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            var allItems = await db.ImageContents.ToListAsync();

            var loopCount = 0;
            var totalCount = allItems.Count;

            StatusContext.Progress($"Found {totalCount} Images to Clean and Resize");

            foreach (var loopItem in allItems)
            {
                StatusContext.Progress($"Clean and Resize for {loopItem.Title} - {loopCount} of {totalCount}");

                var fileCheck = PictureResizing.CopyCleanResizeImage(loopItem, StatusContext.ProgressTracker());

                if (!fileCheck.Item1)
                    await StatusContext.ShowMessage("File Error",
                        $"There was an error processing image {loopItem.Title} " +
                        $"- {fileCheck.Item2} - after you hit Ok processing will continue but" +
                        " the process may error...", new List<string> {"Ok"});

                loopCount++;
            }
        }


        private async Task CleanAndResizeAllPhotoFiles()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            var allItems = await db.PhotoContents.ToListAsync();

            var loopCount = 0;
            var totalCount = allItems.Count;

            StatusContext.Progress($"Found {totalCount} Photos to Clean and Resize");

            foreach (var loopItem in allItems)
            {
                StatusContext.Progress($"Clean and Resize for {loopItem.Title} - {loopCount} of {totalCount}");

                var fileCheck = PictureResizing.CopyCleanResizePhoto(loopItem, StatusContext.ProgressTracker());

                if (!fileCheck.Item1)
                    await StatusContext.ShowMessage("File Error",
                        $"There was an error processing photo {loopItem.Title} " +
                        $"- {fileCheck.Item2} - after you hit Ok processing will continue but" +
                        " the process may error...", new List<string> {"Ok"});

                loopCount++;
            }
        }

        private async Task CleanAndResizePictures()
        {
            await CleanAndResizeAllPhotoFiles();
            await CleanAndResizeAllImageFiles();
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

                var originalFileCheck = PictureResizing.CheckFileOriginalFileIsInMediaAndContentDirectories(loopItem);

                if (!originalFileCheck.Item1)
                    await StatusContext.ShowMessage("File Error",
                        $"There was an error processing file item {loopItem.Title} " +
                        $"- {originalFileCheck.Item2} - after you hit Ok processing will continue but" +
                        " the process may error...", new List<string> {"Ok"});

                var htmlModel = new SingleFilePage(loopItem);
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem, StatusContext.ProgressTracker());

                loopCount++;
            }
        }

        private async Task GenerateAllHtml()
        {
            await GenerateAllImageHtml();
            await GenerateAllPhotoHtml();
            await GenerateAllFileHtml();
            await GenerateAllNoteHtml();
            await GenerateAllPostHtml();
            await GenerateAllListHtml();
            await GenerateIndex();
        }

        private async Task GenerateAllHtmlAndCleanAndResizePictures()
        {
            await CleanAndResizePictures();

            await GenerateAllHtml();
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
                await Export.WriteLocalDbJson(loopItem);

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

            var linkListPage = new LinkListPage();
            linkListPage.WriteLocalHtmlRssAndJson();
            Export.WriteLinkListJson();
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
                await Export.WriteLocalDbJson(loopItem);

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
                await Export.WriteLocalDbJson(loopItem);

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
                await Export.WriteLocalDbJson(loopItem);

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

        private async Task ImportJsonFromDirectory()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Starting JSON load.");

            var dialog = new VistaFolderBrowserDialog();

            if (!(dialog.ShowDialog() ?? false)) return;

            var newDirectory = new DirectoryInfo(dialog.SelectedPath);

            if (!newDirectory.Exists)
            {
                StatusContext.ToastError("Directory doesn't exist?");
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            Import.FullImportFromRootDirectory(newDirectory, StatusContext.ProgressTracker());

            StatusContext.Progress("JSON Import Finished");
        }

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            TabImageListContext = new ImageListWithActionsContext(null);
            TabFileListContext = new FileListWithActionsContext(null);
            TabPhotoListContext = new PhotoListWithActionsContext(null);
            TabPostListContext = new PostListWithActionsContext(null);
            TabNoteListContext = new NoteListWithActionsContext(null);
            LinkStreamContext = new LinkStreamListWithActionsContext(null);
            SettingsEditorContext =
                new UserSettingsEditorContext(StatusContext, UserSettingsSingleton.CurrentSettings());
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