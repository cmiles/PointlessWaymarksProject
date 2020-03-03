using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.ImageHtml;
using PointlessWaymarksCmsData.JsonFiles;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.ShowInSiteContentEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarksCmsWpfControls.UpdateNotesEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.ImageContentEditor
{
    public class ImageContentEditorContext : INotifyPropertyChanged
    {
        private string _altText;
        private Command _chooseFileCommand;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private ImageContent _dbEntry;
        private string _imageSourceNotes;
        private Command _resizeFileCommand;
        private Command _saveAndCreateLocalCommand;
        private Command _saveAndGenerateHtmlCommand;
        private Command _saveUpdateDatabaseCommand;
        private FileInfo _selectedFile;
        private string _selectedFileFullPath;
        private ShowInMainSiteFeedEditorContext _showInSiteFeed;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tagEdit;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;
        private Command _viewOnSiteCommand;

        public ImageContentEditorContext(StatusControlContext statusContext, ImageContent toLoad)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(toLoad));
        }

        public string AltText
        {
            get => _altText;
            set
            {
                if (value == _altText) return;
                _altText = value;
                OnPropertyChanged();
            }
        }

        public Command ChooseFileCommand
        {
            get => _chooseFileCommand;
            set
            {
                if (Equals(value, _chooseFileCommand)) return;
                _chooseFileCommand = value;
                OnPropertyChanged();
            }
        }

        public ContentIdViewerControlContext ContentId
        {
            get => _contentId;
            set
            {
                if (Equals(value, _contentId)) return;
                _contentId = value;
                OnPropertyChanged();
            }
        }

        public CreatedAndUpdatedByAndOnDisplayContext CreatedUpdatedDisplay
        {
            get => _createdUpdatedDisplay;
            set
            {
                if (Equals(value, _createdUpdatedDisplay)) return;
                _createdUpdatedDisplay = value;
                OnPropertyChanged();
            }
        }

        public ImageContent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public string ImageSourceNotes
        {
            get => _imageSourceNotes;
            set
            {
                if (value == _imageSourceNotes) return;
                _imageSourceNotes = value;
                OnPropertyChanged();
            }
        }

        public Command ResizeFileCommand
        {
            get => _resizeFileCommand;
            set
            {
                if (Equals(value, _resizeFileCommand)) return;
                _resizeFileCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SaveAndCreateLocalCommand
        {
            get => _saveAndCreateLocalCommand;
            set
            {
                if (Equals(value, _saveAndCreateLocalCommand)) return;
                _saveAndCreateLocalCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SaveAndGenerateHtmlCommand
        {
            get => _saveAndGenerateHtmlCommand;
            set
            {
                if (Equals(value, _saveAndGenerateHtmlCommand)) return;
                _saveAndGenerateHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SaveUpdateDatabaseCommand
        {
            get => _saveUpdateDatabaseCommand;
            set
            {
                if (Equals(value, _saveUpdateDatabaseCommand)) return;
                _saveUpdateDatabaseCommand = value;
                OnPropertyChanged();
            }
        }

        public FileInfo SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (Equals(value, _selectedFile)) return;
                _selectedFile = value;
                OnPropertyChanged();

                if (SelectedFile == null) return;
                SelectedFileFullPath = SelectedFile.FullName;
            }
        }

        public string SelectedFileFullPath
        {
            get => _selectedFileFullPath;
            set
            {
                if (value == _selectedFileFullPath) return;
                _selectedFileFullPath = value;
                OnPropertyChanged();
            }
        }

        public ShowInMainSiteFeedEditorContext ShowInSiteFeed
        {
            get => _showInSiteFeed;
            set
            {
                if (Equals(value, _showInSiteFeed)) return;
                _showInSiteFeed = value;
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

        public TagsEditorContext TagEdit
        {
            get => _tagEdit;
            set
            {
                if (Equals(value, _tagEdit)) return;
                _tagEdit = value;
                OnPropertyChanged();
            }
        }

        public TitleSummarySlugEditorContext TitleSummarySlugFolder
        {
            get => _titleSummarySlugFolder;
            set
            {
                if (Equals(value, _titleSummarySlugFolder)) return;
                _titleSummarySlugFolder = value;
                OnPropertyChanged();
            }
        }

        public UpdateNotesEditorContext UpdateNotes
        {
            get => _updateNotes;
            set
            {
                if (Equals(value, _updateNotes)) return;
                _updateNotes = value;
                OnPropertyChanged();
            }
        }

        public Command ViewOnSiteCommand
        {
            get => _viewOnSiteCommand;
            set
            {
                if (Equals(value, _viewOnSiteCommand)) return;
                _viewOnSiteCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task ChooseFile()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Starting image load.");

            var dialog = new VistaOpenFileDialog();

            if (!(dialog.ShowDialog() ?? false)) return;

            var newFile = new FileInfo(dialog.FileName);

            if (!newFile.Exists)
            {
                StatusContext.ToastError("File doesn't exist?");
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            SelectedFile = newFile;

            StatusContext.Progress($"Image load - {SelectedFile.FullName} ");
        }

        private async Task GenerateHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var htmlContext = new SingleImagePage(DbEntry);

            htmlContext.WriteLocalHtml();
        }

        private async Task LoadData(ImageContent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new ImageContent();
            TitleSummarySlugFolder = new TitleSummarySlugEditorContext(StatusContext, toLoad);
            ShowInSiteFeed = new ShowInMainSiteFeedEditorContext(StatusContext, toLoad, false);
            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, toLoad);
            ContentId = new ContentIdViewerControlContext(StatusContext, toLoad);
            UpdateNotes = new UpdateNotesEditorContext(StatusContext, toLoad);
            TagEdit = new TagsEditorContext(StatusContext, toLoad);

            if (toLoad != null && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName))
            {
                PictureResizing.CheckImageOriginalFileIsInMediaAndContentDirectories(DbEntry);

                var archiveFile = new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalMasterMediaArchiveImageDirectory().FullName,
                    toLoad.OriginalFileName));

                if (archiveFile.Exists)
                    SelectedFile = archiveFile;
                else
                    await StatusContext.ShowMessage("Missing Photo",
                        $"There is an original image file listed for this image - {DbEntry.OriginalFileName} -" +
                        $" but it was not found in the expected location of {archiveFile.FullName} - " +
                        "this will cause an error and prevent you from saving. You can re-load the image or " +
                        "maybe your master media directory moved unexpectedly and you could close this editor " +
                        "and restore it (or change it in settings) before continuing?", new List<string> {"OK"});
            }

            ImageSourceNotes = DbEntry.ImageSourceNotes ?? string.Empty;
            AltText = DbEntry.AltText ?? string.Empty;

            ChooseFileCommand = new Command(() => StatusContext.RunBlockingTask(async () => await ChooseFile()));
            ResizeFileCommand = new Command(() => StatusContext.RunBlockingTask(ResizeImage));
            SaveAndGenerateHtmlCommand = new Command(() => StatusContext.RunBlockingTask(SaveAndGenerateHtml));
            SaveAndCreateLocalCommand = new Command(() => StatusContext.RunBlockingTask(SaveAndCreateLocal));
            SaveUpdateDatabaseCommand = new Command(() => StatusContext.RunBlockingTask(SaveToDbWithValidation));
            ViewOnSiteCommand = new Command(() => StatusContext.RunBlockingTask(ViewOnSite));
        }

        private DirectoryInfo LocalContentDirectory(UserSettings settings)
        {
            var imageDirectory =
                new DirectoryInfo(Path.Combine(LocalFolderDirectory(settings).FullName, TitleSummarySlugFolder.Slug));
            if (!imageDirectory.Exists) imageDirectory.Create();

            imageDirectory.Refresh();

            return imageDirectory;
        }

        private DirectoryInfo LocalFolderDirectory(UserSettings settings)
        {
            var folderDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteImageDirectory().FullName,
                TitleSummarySlugFolder.Folder));
            if (!folderDirectory.Exists) folderDirectory.Create();

            folderDirectory.Refresh();

            return folderDirectory;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task ResizeImage()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedFile == null)
            {
                StatusContext.ToastError("Can't Resize - No File?");
                return;
            }

            SelectedFile.Refresh();

            if (!SelectedFile.Exists)
            {
                StatusContext.ToastError("Can't Resize - No File?");
                return;
            }

            PictureResizing.ResizeForDisplayAndSrcset(SelectedFile, true, StatusContext.ProgressTracker());
        }

        private async Task SaveAndCreateLocal()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var validationList = await ValidateAll();

            if (validationList.Any(x => !x.Item1))
            {
                await StatusContext.ShowMessage("Validation Error",
                    string.Join(Environment.NewLine, validationList.Where(x => !x.Item1).Select(x => x.Item2).ToList()),
                    new List<string> {"Ok"});
                return;
            }

            await SaveToDatabase();
            await WriteSelectedFileToMasterMediaArchive();
            await WriteSelectedFileToLocalSite();
            await GenerateHtml();
            await Export.WriteLocalDbJson(DbEntry);
        }


        public async Task SaveAndGenerateHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var validationList = await ValidateAll();

            if (validationList.Any(x => !x.Item1))
            {
                await StatusContext.ShowMessage("Validation Error",
                    string.Join(Environment.NewLine, validationList.Where(x => !x.Item1).Select(x => x.Item2).ToList()),
                    new List<string> {"Ok"});
                return;
            }

            await SaveToDatabase();
            await GenerateHtml();
            await Export.WriteLocalDbJson(DbEntry);
        }

        private async Task SaveToDatabase()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newEntry = new ImageContent();

            if (DbEntry == null || DbEntry.Id < 1)
            {
                newEntry.ContentId = Guid.NewGuid();
                newEntry.CreatedOn = DateTime.Now;
            }
            else
            {
                newEntry.ContentId = DbEntry.ContentId;
                newEntry.CreatedOn = DbEntry.CreatedOn;
                newEntry.LastUpdatedOn = DateTime.Now;
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedBy;
            }

            newEntry.MainPicture = newEntry.ContentId;
            newEntry.Folder = TitleSummarySlugFolder.Folder;
            newEntry.Slug = TitleSummarySlugFolder.Slug;
            newEntry.Summary = TitleSummarySlugFolder.Summary;
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.ShowInMainSite;
            newEntry.Tags = TagEdit.Tags;
            newEntry.Title = TitleSummarySlugFolder.Title;
            newEntry.AltText = AltText;
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedBy;
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes;
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.OriginalFileName = SelectedFile.Name;
            newEntry.ImageSourceNotes = ImageSourceNotes;

            if (DbEntry != null && DbEntry.Id > 0)
                if (DbEntry.Slug != newEntry.Slug || DbEntry.Folder != newEntry.Folder)
                {
                    var settings = UserSettingsSingleton.CurrentSettings();
                    var existingDirectory = settings.LocalSiteImageContentDirectory(DbEntry, false);

                    if (existingDirectory.Exists)
                    {
                        var newDirectory =
                            new DirectoryInfo(settings.LocalSiteImageContentDirectory(newEntry, false).FullName);
                        existingDirectory.MoveTo(settings.LocalSiteImageContentDirectory(newEntry, false).FullName);
                        newDirectory.Refresh();

                        var possibleOldHtmlFile =
                            new FileInfo($"{Path.Combine(newDirectory.FullName, DbEntry.Slug)}.html");
                        if (possibleOldHtmlFile.Exists)
                            possibleOldHtmlFile.MoveTo(settings.LocalSiteImageHtmlFile(newEntry).FullName);
                    }
                }

            var context = await Db.Context();

            var toHistoric = await context.ImageContents.Where(x => x.ContentId == newEntry.ContentId).ToListAsync();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricImageContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                await context.HistoricImageContents.AddAsync(newHistoric);
                context.ImageContents.Remove(loopToHistoric);
            }

            context.ImageContents.Add(newEntry);

            await context.SaveChangesAsync(true);

            DbEntry = newEntry;

            await LoadData(newEntry);
        }

        private async Task SaveToDbWithValidation()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var validationList = await ValidateAll();

            if (validationList.Any(x => !x.Item1))
            {
                await StatusContext.ShowMessage("Validation Error",
                    string.Join(Environment.NewLine, validationList.Where(x => !x.Item1).Select(x => x.Item2).ToList()),
                    new List<string> {"Ok"});
                return;
            }

            await SaveToDatabase();
        }

        private async Task<(bool, string)> Validate()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            SelectedFile.Refresh();

            if (!SelectedFile.Exists) return (false, "File doesn't exist?");

            if (!(SelectedFile.Extension.ToLower().Contains("jpg") ||
                  SelectedFile.Extension.ToLower().Contains("jpeg")))
                return (false, "The file doesn't appear to be a supported file type.");

            if (await (await Db.Context()).ImageFilenameExistsInDatabase(SelectedFile.Name, DbEntry?.ContentId))
                return (false, "This filename already exists in the database - image file names must be unique.");

            return (true, string.Empty);
        }

        private async Task<List<(bool, string)>> ValidateAll()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            return new List<(bool, string)>
            {
                await UserSettingsUtilities.ValidateLocalSiteRootDirectory(),
                await UserSettingsUtilities.ValidateLocalMasterMediaArchive(),
                await TitleSummarySlugFolder.Validate(),
                await CreatedUpdatedDisplay.Validate(),
                await Validate()
            };
        }

        private async Task ViewOnSite()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (DbEntry == null || DbEntry.Id < 1)
            {
                StatusContext.ToastError("Please save the content first...");
                return;
            }

            var settings = UserSettingsSingleton.CurrentSettings();

            var url = $@"http://{settings.ImagePageUrl(DbEntry)}";

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        private async Task WriteSelectedFileToLocalSite()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var userSettings = UserSettingsSingleton.CurrentSettings();

            var targetDirectory = LocalContentDirectory(userSettings);

            var originalFileInTargetDirectoryFullName = Path.Combine(targetDirectory.FullName, SelectedFile.Name);

            var sourceImage = new FileInfo(originalFileInTargetDirectoryFullName);

            if (originalFileInTargetDirectoryFullName != SelectedFile.FullName)
            {
                if (sourceImage.Exists) sourceImage.Delete();
                SelectedFile.CopyTo(originalFileInTargetDirectoryFullName);
                sourceImage.Refresh();
            }

            PictureResizing.ResizeForDisplayAndSrcset(sourceImage, true, StatusContext.ProgressTracker());
        }

        private async Task WriteSelectedFileToMasterMediaArchive()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var userSettings = UserSettingsSingleton.CurrentSettings();
            var destinationFileName = Path.Combine(userSettings.LocalMasterMediaArchiveImageDirectory().FullName,
                SelectedFile.Name);
            if (destinationFileName == SelectedFile.FullName) return;

            var destinationFile = new FileInfo(destinationFileName);

            if (destinationFile.Exists) destinationFile.Delete();

            SelectedFile.CopyTo(destinationFileName);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}