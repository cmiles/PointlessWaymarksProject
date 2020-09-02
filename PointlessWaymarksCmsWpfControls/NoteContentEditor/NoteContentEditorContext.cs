using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.BodyContentEditor;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.ShowInMainSiteFeedEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.NoteContentEditor
{
    public class NoteContentEditorContext : INotifyPropertyChanged, IHasChanges
    {
        private BodyContentEditorContext _bodyContent;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private NoteContent _dbEntry;
        private Command _extractNewLinksCommand;
        private string _folder;
        private bool _folderHasChanges;
        private Command _saveAndCreateLocalCommand;
        private ShowInMainSiteFeedEditorContext _showInSiteFeed;
        private string _slug;
        private string _summary;
        private bool _summaryHasChanges;
        private TagsEditorContext _tagEdit;
        private Command _viewOnSiteCommand;

        public NoteContentEditorContext(StatusControlContext statusContext, NoteContent noteContent)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            SaveAndCreateLocalCommand = StatusContext.RunBlockingTaskCommand(SaveAndGenerateHtml);
            ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
                LinkExtraction.ExtractNewAndShowLinkContentEditors(BodyContent.BodyContent,
                    StatusContext.ProgressTracker()));

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(noteContent));
        }

        private void CheckForChanges()
        {
            // ReSharper disable InvokeAsExtensionMethod - in this case TrimNullSage - which returns an
            //Empty string from null will not be invoked as an extension if DbEntry is null...
            SummaryHasChanges = StringHelpers.TrimNullToEmpty(DbEntry?.Summary) != Summary.TrimNullToEmpty();
            FolderHasChanges = StringHelpers.TrimNullToEmpty(DbEntry?.Folder) != Folder.TrimNullToEmpty();
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

        public bool HasChanges =>
            !(StringHelpers.AreEqual(DbEntry.Folder, Folder.TrimNullToEmpty()) &&
              StringHelpers.AreEqual(DbEntry.Summary, Summary.TrimNullToEmpty()) &&
              StringHelpers.AreEqual(DbEntry.CreatedBy, CreatedUpdatedDisplay.CreatedByEntry.UserValue) &&
              StringHelpers.AreEqual(DbEntry.BodyContent, BodyContent.BodyContent) &&
              StringHelpers.AreEqual(DbEntry.BodyContentFormat,
                  BodyContent.BodyContentFormat.SelectedContentFormatAsString) && !TagEdit.TagsHaveChanges &&
              DbEntry.ShowInMainSiteFeed == ShowInSiteFeed.ShowInMainSiteFeed);

        public event PropertyChangedEventHandler PropertyChanged;

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
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges")) CheckForChanges();
        }

        private NoteContent CurrentStateToFileContent()
        {
            var newEntry = new NoteContent();

            if (DbEntry == null || DbEntry.Id < 1)
            {
                newEntry.ContentId = Guid.NewGuid();
                newEntry.Slug = NoteGenerator.UniqueNoteSlug().Result;
                newEntry.CreatedOn = DateTime.Now;
            }
            else
            {
                newEntry.Slug = Slug.TrimNullToEmpty();
                newEntry.ContentId = DbEntry.ContentId;
                newEntry.CreatedOn = DbEntry.CreatedOn;
                newEntry.LastUpdatedOn = DateTime.Now;
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedByEntry.UserValue.TrimNullToEmpty();
            }

            newEntry.Folder = Folder.TrimNullToEmpty();
            newEntry.Summary = Summary.TrimNullToEmpty();
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.ShowInMainSiteFeed;
            newEntry.Tags = TagEdit.TagListString();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedByEntry.UserValue.TrimNullToEmpty();
            newEntry.BodyContent = BodyContent.BodyContent.TrimNullToEmpty();
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;

            return newEntry;
        }

        public async Task SaveAndGenerateHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var (generationReturn, newContent) = await NoteGenerator.SaveAndGenerateHtml(CurrentStateToFileContent(),
                null, StatusContext.ProgressTracker());

            if (generationReturn.HasError || newContent == null)
            {
                await StatusContext.ShowMessageWithOkButton("Problem Saving and Generating Html",
                    generationReturn.GenerationNote);
                return;
            }

            await LoadData(newContent);
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