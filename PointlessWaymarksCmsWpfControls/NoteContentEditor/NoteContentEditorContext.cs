using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.NoteHtml;
using PointlessWaymarksCmsWpfControls.BodyContentEditor;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.UpdateNotesEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.NoteContentEditor
{
    public class NoteContentEditorContext : INotifyPropertyChanged
    {
        private BodyContentEditorContext _bodyContent;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private NoteContent _dbEntry;
        private string _folder;
        private RelayCommand _saveAndCreateLocalCommand;
        private RelayCommand _saveUpdateDatabaseCommand;
        private bool _showInSiteFeed;
        private string _slug;
        private TagsEditorContext _tagEdit;
        private UpdateNotesEditorContext _updateNotes;
        private RelayCommand _viewOnSiteCommand;
        private string _summary;

        public NoteContentEditorContext(StatusControlContext statusContext, NoteContent noteContent)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(noteContent));
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

        public NoteContent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public string Folder
        {
            get => _folder;
            set
            {
                if (value == _folder) return;
                _folder = value;
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

        public bool ShowInSiteFeed
        {
            get => _showInSiteFeed;
            set
            {
                if (value == _showInSiteFeed) return;
                _showInSiteFeed = value;
                OnPropertyChanged();
            }
        }

        public string Slug
        {
            get => _slug;
            set
            {
                if (value == _slug) return;
                _slug = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext { get; set; }

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

        public RelayCommand ViewOnSiteCommand
        {
            get => _viewOnSiteCommand;
            set
            {
                if (Equals(value, _viewOnSiteCommand)) return;
                _viewOnSiteCommand = value;
                OnPropertyChanged();
            }
        }

        private async Task GenerateHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var htmlContext = new SingleNotePage(DbEntry);

            htmlContext.WriteLocalHtml();
        }

        public async Task LoadData(NoteContent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new NoteContent();
            Folder = toLoad?.Folder ?? string.Empty;
            Summary = toLoad?.Summary ?? string.Empty;
            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, toLoad);
            ContentId = new ContentIdViewerControlContext(StatusContext, toLoad);
            TagEdit = new TagsEditorContext(StatusContext, toLoad);
            BodyContent = new BodyContentEditorContext(StatusContext, toLoad);
            ShowInSiteFeed = toLoad?.ShowInSiteFeed ?? true;

            if (string.IsNullOrWhiteSpace(toLoad?.Slug))
            {
                var possibleSlug = SlugUtility.RandomLowerCaseString(6);

                var db = await Db.Context();

                async Task<bool> SlugAlreadyExists(string slug)
                {
                    return await db.NoteContents.AnyAsync(x => x.Slug == slug);
                }

                while (await SlugAlreadyExists(possibleSlug)) possibleSlug = SlugUtility.RandomLowerCaseString(6);

                Slug = possibleSlug;
            }
            else
            {
                Slug = toLoad.Slug;
            }

            SaveAndCreateLocalCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SaveAndCreateLocal));
            SaveUpdateDatabaseCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SaveToDbWithValidation));
            ViewOnSiteCommand = new RelayCommand(() => StatusContext.RunBlockingTask(ViewOnSite));
        }

        public string Summary
        {
            get => _summary;
            set
            {
                if (value == _summary) return;
                _summary = value;
                OnPropertyChanged();
            }
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

            var newEntry = new NoteContent();

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

            newEntry.Slug = Slug;
            newEntry.Folder = Folder;
            newEntry.Summary = Summary;
            newEntry.Tags = TagEdit.Tags;
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedBy;
            newEntry.BodyContent = BodyContent.BodyContent;
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
            newEntry.ShowInSiteFeed = ShowInSiteFeed;

            if (DbEntry != null && DbEntry.Id > 0)
                if (DbEntry.Slug != newEntry.Slug || DbEntry.Folder != newEntry.Folder)
                {
                    var settings = UserSettingsSingleton.CurrentSettings();
                    var existingDirectory = settings.LocalSiteNoteContentDirectory(DbEntry, false);

                    if (existingDirectory.Exists)
                    {
                        var newDirectory =
                            new DirectoryInfo(settings.LocalSiteNoteContentDirectory(newEntry, false).FullName);
                        existingDirectory.MoveTo(settings.LocalSiteNoteContentDirectory(newEntry, false).FullName);
                        newDirectory.Refresh();

                        var possibleOldHtmlFile =
                            new FileInfo($"{Path.Combine(newDirectory.FullName, DbEntry.Slug)}.html");
                        if (possibleOldHtmlFile.Exists)
                            possibleOldHtmlFile.MoveTo(settings.LocalSiteNoteHtmlFile(newEntry).FullName);
                    }
                }

            var context = await Db.Context();

            var toHistoric = await context.NoteContents.Where(x => x.ContentId == newEntry.ContentId).ToListAsync();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricNoteContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                await context.HistoricNoteContents.AddAsync(newHistoric);
                context.NoteContents.Remove(loopToHistoric);
            }

            context.NoteContents.Add(newEntry);

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

            var isValid = true;
            var errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Slug))
            {
                isValid = false;
                errorMessage += "Slug can not be blank.";
            }

            if (string.IsNullOrWhiteSpace(Folder))
            {
                isValid = false;
                errorMessage += "Folder can not be blank.";
            }

            if (string.IsNullOrWhiteSpace(Summary))
            {
                isValid = false;
                errorMessage += "Summary can not be blank.";
            }
            
            if (!isValid) return (false, errorMessage);

            if (!FolderFileUtility.IsValidFilename(Folder))
            {
                isValid = false;
                errorMessage += "Folders have illegal characters...";
            }

            if (!isValid) return (false, errorMessage);

            if (await (await Db.Context()).SlugExistsInDatabase(Slug))
            {
                isValid = false;
                errorMessage += "Slug already exists in Database";
            }

            return (isValid, errorMessage);
        }

        private async Task<List<(bool, string)>> ValidateAll()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            return new List<(bool, string)>
            {
                await UserSettingsUtilities.ValidateLocalSiteRootDirectory(),
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

            var url = $@"http://{settings.NotePageUrl(DbEntry)}";

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        private async Task WriteLocalDbJson()
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(DbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteNoteContentDirectory(DbEntry).FullName,
                $"{DbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricNoteContents.Where(x => x.ContentId == DbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10);

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteNoteContentDirectory(DbEntry).FullName,
                $"{DbEntry.ContentId}-Historic.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            File.WriteAllText(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}