using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.FileHtml;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.BodyContentEditor;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarksCmsWpfControls.UpdateNotesEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.FileContentEditor
{
    public class FileContentEditorContext : INotifyPropertyChanged
    {
        private BodyContentEditorContext _bodyContent;
        private RelayCommand _chooseFileCommand;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private FileContent _dbEntry;
        private RelayCommand _saveAndCreateLocalCommand;
        private RelayCommand _saveUpdateDatabaseCommand;
        private FileInfo _selectedFile;
        private string _selectedFileFullPath;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tagEdit;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;

        public FileContentEditorContext(StatusControlContext statusContext, FileContent toLoad)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(toLoad));
        }

        public BodyContentEditorContext BodyContent
        {
            get => _bodyContent;
            set
            {
                if (Equals(value, _bodyContent)) return;
                _bodyContent = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ChooseFileCommand
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

        public FileContent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand SaveAndCreateLocalCommand
        {
            get => _saveAndCreateLocalCommand;
            set
            {
                if (Equals(value, _saveAndCreateLocalCommand)) return;
                _saveAndCreateLocalCommand = value;
                OnPropertyChanged();
            }
        }


        public RelayCommand SaveUpdateDatabaseCommand
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

            StatusContext.Progress($"File load - {SelectedFile.FullName} ");
        }

        private async Task GenerateHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var htmlContext = new SingleFilePage(DbEntry);

            htmlContext.WriteLocalHtml();
        }

        private async Task LoadData(FileContent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new FileContent();
            TitleSummarySlugFolder = new TitleSummarySlugEditorContext(StatusContext, toLoad);
            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, toLoad);
            ContentId = new ContentIdViewerControlContext(StatusContext, toLoad);
            UpdateNotes = new UpdateNotesEditorContext(StatusContext, toLoad);
            TagEdit = new TagsEditorContext(StatusContext, toLoad);
            BodyContent = new BodyContentEditorContext(StatusContext, toLoad);

            ChooseFileCommand = new RelayCommand(() => StatusContext.RunBlockingTask(async () => await ChooseFile()));
            SaveAndCreateLocalCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SaveAndCreateLocal));
            SaveUpdateDatabaseCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SaveToDbWithValidation));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task SaveAndCreateLocal()
        {
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
            await WriteLocalDbJson();
        }


        private async Task SaveToDatabase()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newEntry = new FileContent();

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

            newEntry.Folder = TitleSummarySlugFolder.Folder;
            newEntry.Slug = TitleSummarySlugFolder.Slug;
            newEntry.Summary = TitleSummarySlugFolder.Summary;
            newEntry.Tags = TagEdit.Tags;
            newEntry.Title = TitleSummarySlugFolder.Title;
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedBy;
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes;
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.BodyContent = BodyContent.BodyContent;
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;

            var context = await Db.Context();

            var toHistoric = await context.FileContents.Where(x => x.ContentId == newEntry.ContentId).ToListAsync();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricFileContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                await context.HistoricFileContents.AddAsync(newHistoric);
                context.FileContents.Remove(loopToHistoric);
            }

            context.FileContents.Add(newEntry);

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

            return (true, string.Empty);
        }

        private async Task<List<(bool, string)>> ValidateAll()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            return new List<(bool, string)>
            {
                await UserSettingsUtilities.ValidateLocalSiteRootDirectory(),
                await TitleSummarySlugFolder.Validate(),
                await CreatedUpdatedDisplay.Validate(),
                await Validate()
            };
        }

        private async Task WriteLocalDbJson()
        {
            var settings = await UserSettingsUtilities.ReadSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(DbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteFileContentDirectory(DbEntry).FullName,
                $"{DbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricFileContents.Where(x => x.ContentId == DbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10);

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteFileContentDirectory(DbEntry).FullName,
                $"{DbEntry.ContentId}-Historic.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            File.WriteAllText(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}