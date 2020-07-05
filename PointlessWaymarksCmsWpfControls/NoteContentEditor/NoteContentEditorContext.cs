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
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.JsonFiles;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.NoteHtml;
using PointlessWaymarksCmsWpfControls.BodyContentEditor;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.ShowInMainSiteFeedEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.UpdateNotesEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.NoteContentEditor
{
    public class NoteContentEditorContext : INotifyPropertyChanged, IHasUnsavedChanges
    {
        private BodyContentEditorContext _bodyContent;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private NoteContent _dbEntry;
        private Command _extractNewLinksCommand;
        private string _folder;
        private bool _folderHasChanges;
        private Command _saveAndCreateLocalCommand;
        private Command _saveUpdateDatabaseCommand;
        private ShowInMainSiteFeedEditorContext _showInSiteFeed;
        private string _slug;
        private string _summary;
        private bool _summaryHasChanges;
        private TagsEditorContext _tagEdit;
        private UpdateNotesEditorContext _updateNotes;
        private Command _viewOnSiteCommand;

        public NoteContentEditorContext(StatusControlContext statusContext, NoteContent noteContent)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            SaveAndCreateLocalCommand = new Command(() => StatusContext.RunBlockingTask(SaveAndCreateLocal));
            SaveUpdateDatabaseCommand = new Command(() => StatusContext.RunBlockingTask(SaveToDbWithValidation));
            ViewOnSiteCommand = new Command(() => StatusContext.RunBlockingTask(ViewOnSite));
            ExtractNewLinksCommand = new Command(() => StatusContext.RunBlockingTask(() =>
                LinkExtraction.ExtractNewAndShowLinkStreamEditors(BodyContent.BodyContent,
                    StatusContext.ProgressTracker())));

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(noteContent));
        }

        private void CheckForChanges()
        {
            // ReSharper disable InvokeAsExtensionMethod - in this case TrimNullSage - which returns an
            //Empty string from null will not be invoked as an extension if DbEntry is null...
            SummaryHasChanges = StringHelper.TrimNullSafe(DbEntry?.Summary) != Summary.TrimNullSafe();
            FolderHasChanges = StringHelper.TrimNullSafe(DbEntry?.Folder) != Folder.TrimNullSafe();
            // ReSharper restore InvokeAsExtensionMethod
        }

        public bool FolderHasChanges
        {
            get => _folderHasChanges;
            set
            {
                if (value == _folderHasChanges) return;
                _folderHasChanges = value;
                OnPropertyChanged();
            }
        }

        public bool SummaryHasChanges
        {
            get => _summaryHasChanges;
            set
            {
                if (value == _summaryHasChanges) return;
                _summaryHasChanges = value;
                OnPropertyChanged();
            }
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

        public Command ExtractNewLinksCommand
        {
            get => _extractNewLinksCommand;
            set
            {
                if (Equals(value, _extractNewLinksCommand)) return;
                _extractNewLinksCommand = value;
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

        public ShowInMainSiteFeedEditorContext ShowInSiteFeed
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

        public bool HasChanges()
        {
            return !(StringHelper.AreEqual(DbEntry.Folder, Folder.TrimNullSafe()) &&
                     StringHelper.AreEqual(DbEntry.Summary, Summary.TrimNullSafe()) &&
                     StringHelper.AreEqual(DbEntry.CreatedBy, CreatedUpdatedDisplay.CreatedBy) &&
                     StringHelper.AreEqual(DbEntry.BodyContent, BodyContent.BodyContent) &&
                     StringHelper.AreEqual(DbEntry.BodyContentFormat,
                         BodyContent.BodyContentFormat.SelectedContentFormatAsString) && !TagEdit.TagsHaveChanges &&
                     DbEntry.ShowInMainSiteFeed == ShowInSiteFeed.ShowInMainSite);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private async Task GenerateHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var htmlContext = new SingleNotePage(DbEntry);

            htmlContext.WriteLocalHtml();
        }

        public async Task LoadData(NoteContent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new NoteContent
            {
                BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            };

            Folder = DbEntry?.Folder ?? string.Empty;
            Summary = DbEntry?.Summary ?? string.Empty;
            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, DbEntry);
            ShowInSiteFeed = new ShowInMainSiteFeedEditorContext(StatusContext, DbEntry, true);
            ContentId = new ContentIdViewerControlContext(StatusContext, DbEntry);
            TagEdit = new TagsEditorContext(StatusContext, DbEntry);
            BodyContent = new BodyContentEditorContext(StatusContext, DbEntry);

            if (string.IsNullOrWhiteSpace(DbEntry.Slug))
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
                Slug = DbEntry.Slug;
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (!propertyName.Contains("HasChanges")) CheckForChanges();
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
            await Export.WriteLocalDbJson(DbEntry);
        }


        private async Task SaveToDatabase()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newEntry = new NoteContent();

            var isNewEntry = false;

            if (DbEntry == null || DbEntry.Id < 1)
            {
                isNewEntry = true;
                newEntry.ContentId = Guid.NewGuid();
                newEntry.CreatedOn = DateTime.Now;
                newEntry.ContentVersion = newEntry.CreatedOn.ToUniversalTime();
            }
            else
            {
                newEntry.ContentId = DbEntry.ContentId;
                newEntry.CreatedOn = DbEntry.CreatedOn;
                newEntry.LastUpdatedOn = DateTime.Now;
                newEntry.ContentVersion = newEntry.LastUpdatedOn.Value.ToUniversalTime();
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedBy;
            }

            newEntry.Slug = Slug.TrimNullSafe();
            newEntry.Folder = Folder.TrimNullSafe();
            newEntry.Summary = Summary.TrimNullSafe();
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.ShowInMainSite;
            newEntry.Tags = TagEdit.TagListString();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedBy;
            newEntry.BodyContent = BodyContent.BodyContent;
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;

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

            await Db.SaveNoteContent(newEntry);

            DbEntry = newEntry;

            await LoadData(newEntry);

            if (isNewEntry)
                await DataNotifications.PublishDataNotification(StatusContext.StatusControlContextId.ToString(),
                    DataNotificationContentType.Note, DataNotificationUpdateType.New,
                    new List<Guid> {newEntry.ContentId});
            else
                await DataNotifications.PublishDataNotification(StatusContext.StatusControlContextId.ToString(),
                    DataNotificationContentType.Note, DataNotificationUpdateType.Update,
                    new List<Guid> {newEntry.ContentId});
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

            if (!FolderFileUtility.IsNoUrlEncodingNeededFilename(Folder))
            {
                isValid = false;
                errorMessage += "Folders have illegal characters...";
            }

            if (!isValid) return (false, errorMessage);

            if (await (await Db.Context()).SlugExistsInDatabase(Slug, DbEntry?.ContentId))
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
    }
}