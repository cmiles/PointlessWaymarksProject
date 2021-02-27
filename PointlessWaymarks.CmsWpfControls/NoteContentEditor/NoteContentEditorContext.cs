using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.BoolDataEntry;
using PointlessWaymarks.CmsWpfControls.ContentFolder;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.StringDataEntry;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.NoteContentEditor
{
    public class NoteContentEditorContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues,
        ICheckForChangesAndValidation
    {
        private BodyContentEditorContext _bodyContent;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private NoteContent _dbEntry;
        private Command _extractNewLinksCommand;
        private ContentFolderContext _folderEntry;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private Command _saveAndCloseCommand;
        private Command _saveCommand;
        private BoolDataEntryContext _showInSiteFeed;
        private string _slug;
        private StringDataEntryContext _summary;
        private TagsEditorContext _tagEdit;
        private Command _viewOnSiteCommand;

        public EventHandler RequestContentEditorWindowClose;

        private NoteContentEditorContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            SaveCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(false));
            SaveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
            ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
                LinkExtraction.ExtractNewAndShowLinkContentEditors(BodyContent.BodyContent,
                    StatusContext.ProgressTracker()));
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

        public ContentFolderContext FolderEntry
        {
            get => _folderEntry;
            set
            {
                if (Equals(value, _folderEntry)) return;
                _folderEntry = value;
                OnPropertyChanged();
            }
        }

        public Command SaveAndCloseCommand
        {
            get => _saveAndCloseCommand;
            set
            {
                if (Equals(value, _saveAndCloseCommand)) return;
                _saveAndCloseCommand = value;
                OnPropertyChanged();
            }
        }


        public Command SaveCommand
        {
            get => _saveCommand;
            set
            {
                if (Equals(value, _saveCommand)) return;
                _saveCommand = value;
                OnPropertyChanged();
            }
        }

        public BoolDataEntryContext ShowInSiteFeed
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

        public StringDataEntryContext Summary
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

        public void CheckForChangesAndValidationIssues()
        {
            HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
            HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (value == _hasChanges) return;
                _hasChanges = value;
                OnPropertyChanged();
            }
        }

        public bool HasValidationIssues
        {
            get => _hasValidationIssues;
            set
            {
                if (value == _hasValidationIssues) return;
                _hasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static async Task<NoteContentEditorContext> CreateInstance(StatusControlContext statusContext,
            NoteContent noteContent)
        {
            var newControl = new NoteContentEditorContext(statusContext);
            await newControl.LoadData(noteContent);
            return newControl;
        }

        private NoteContent CurrentStateToFileContent()
        {
            var newEntry = new NoteContent();

            if (DbEntry == null || DbEntry.Id < 1)
            {
                newEntry.ContentId = Guid.NewGuid();
                newEntry.Slug = NoteGenerator.UniqueNoteSlug().Result;
                newEntry.CreatedOn = DbEntry?.CreatedOn ?? DateTime.Now;
                if (newEntry.CreatedOn == DateTime.MinValue) newEntry.CreatedOn = DateTime.Now;
            }
            else
            {
                newEntry.Slug = Slug.TrimNullToEmpty();
                newEntry.ContentId = DbEntry.ContentId;
                newEntry.CreatedOn = DbEntry.CreatedOn;
                newEntry.LastUpdatedOn = DateTime.Now;
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedByEntry.UserValue.TrimNullToEmpty();
            }

            newEntry.Folder = FolderEntry.UserValue.TrimNullToEmpty();
            newEntry.Summary = Summary.UserValue.TrimNullToEmpty();
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.UserValue;
            newEntry.Tags = TagEdit.TagListString();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedByEntry.UserValue.TrimNullToEmpty();
            newEntry.BodyContent = BodyContent.BodyContent.TrimNullToEmpty();
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;

            return newEntry;
        }

        public async Task LoadData(NoteContent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new NoteContent
            {
                BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                Slug = await NoteGenerator.UniqueNoteSlug()
            };

            FolderEntry = await ContentFolderContext.CreateInstance(StatusContext, DbEntry);
            Summary = StringDataEntryContext.CreateSummaryInstance(DbEntry);
            CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
            ShowInSiteFeed = BoolDataEntryContext.CreateInstanceForShowInSiteFeed(DbEntry, true);
            ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
            TagEdit = TagsEditorContext.CreateInstance(StatusContext, DbEntry);
            BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);
            Slug = DbEntry.Slug;

            PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidationIssues();
        }

        public async Task SaveAndGenerateHtml(bool closeAfterSave)
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

            if (closeAfterSave)
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                RequestContentEditorWindowClose?.Invoke(this, new EventArgs());
            }
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