using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PostContentEditor
{
    public class PostContentEditorContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues,
        ICheckForChangesAndValidation
    {
        private BodyContentEditorContext _bodyContent;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private PostContent _dbEntry;
        private Command _extractNewLinksCommand;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private HelpDisplayContext _helpContext;
        private Command _linkToClipboardCommand;
        private ContentSiteFeedAndIsDraftContext _mainSiteFeed;
        private Command _saveAndCloseCommand;
        private Command _saveCommand;
        private TagsEditorContext _tagEdit;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;
        private Command _viewOnSiteCommand;

        public EventHandler RequestContentEditorWindowClose;

        private PostContentEditorContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            SaveCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(false));
            SaveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
            ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
                LinkExtraction.ExtractNewAndShowLinkContentEditors(
                    $"{BodyContent.BodyContent} {UpdateNotes.UpdateNotes}", StatusContext.ProgressTracker()));
            LinkToClipboardCommand = StatusContext.RunBlockingTaskCommand(LinkToClipboard);

            HelpContext = new HelpDisplayContext(new List<string>
            {
                PostEditorHelpText, CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
            });
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

        public PostContent DbEntry
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


        public HelpDisplayContext HelpContext
        {
            get => _helpContext;
            set
            {
                if (Equals(value, _helpContext)) return;
                _helpContext = value;
                OnPropertyChanged();
            }
        }

        public Command LinkToClipboardCommand
        {
            get => _linkToClipboardCommand;
            set
            {
                if (Equals(value, _linkToClipboardCommand)) return;
                _linkToClipboardCommand = value;
                OnPropertyChanged();
            }
        }

        public ContentSiteFeedAndIsDraftContext MainSiteFeed
        {
            get => _mainSiteFeed;
            set
            {
                if (Equals(value, _mainSiteFeed)) return;
                _mainSiteFeed = value;
                OnPropertyChanged();
            }
        }

        public string PostEditorHelpText =>
            @"
### Post Content

Posts are the most 'generic' content type and will, by default, be included on the main page of the site in chronological order.

Notes:
 - This system does not have 'Pages', Posts (or another Content Type) can serve as 'Pages'.
";

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

        public static async Task<PostContentEditorContext> CreateInstance(StatusControlContext statusContext,
            PostContent postContent = null)
        {
            var newControl = new PostContentEditorContext(statusContext);
            await newControl.LoadData(postContent);
            return newControl;
        }

        private PostContent CurrentStateToPostContent()
        {
            var newEntry = new PostContent();

            if (DbEntry == null || DbEntry.Id < 1)
            {
                newEntry.ContentId = Guid.NewGuid();
                newEntry.CreatedOn = DbEntry?.CreatedOn ?? DateTime.Now;
                if (newEntry.CreatedOn == DateTime.MinValue) newEntry.CreatedOn = DateTime.Now;
            }
            else
            {
                newEntry.ContentId = DbEntry.ContentId;
                newEntry.CreatedOn = DbEntry.CreatedOn;
                newEntry.LastUpdatedOn = DateTime.Now;
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedByEntry.UserValue.TrimNullToEmpty();
            }

            newEntry.Folder = TitleSummarySlugFolder.FolderEntry.UserValue.TrimNullToEmpty();
            newEntry.Slug = TitleSummarySlugFolder.SlugEntry.UserValue.TrimNullToEmpty();
            newEntry.Summary = TitleSummarySlugFolder.SummaryEntry.UserValue.TrimNullToEmpty();
            newEntry.ShowInMainSiteFeed = MainSiteFeed.ShowInMainSiteFeedEntry.UserValue;
            newEntry.FeedOn = MainSiteFeed.FeedOnEntry.UserValue;
            newEntry.IsDraft = MainSiteFeed.IsDraftEntry.UserValue;
            newEntry.Tags = TagEdit.TagListString();
            newEntry.Title = TitleSummarySlugFolder.TitleEntry.UserValue.TrimNullToEmpty();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedByEntry.UserValue.TrimNullToEmpty();
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes.TrimNullToEmpty();
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.BodyContent = BodyContent.BodyContent.TrimNullToEmpty();
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;

            return newEntry;
        }

        private async Task LinkToClipboard()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (DbEntry == null || DbEntry.Id < 1)
            {
                StatusContext.ToastError("Sorry - please save before getting link...");
                return;
            }

            var linkString = BracketCodePosts.Create(DbEntry);

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(linkString);

            StatusContext.ToastSuccess($"To Clipboard: {linkString}");
        }

        public async Task LoadData(PostContent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var created = DateTime.Now;

            DbEntry = toLoad ?? new PostContent
            {
                BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                ShowInMainSiteFeed = true,
                CreatedOn = created,
                FeedOn = created
            };

            TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry);
            CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
            MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
            ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
            UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
            TagEdit = TagsEditorContext.CreateInstance(StatusContext, DbEntry);
            BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);

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

            var (generationReturn, newContent) = await PostGenerator.SaveAndGenerateHtml(CurrentStateToPostContent(),
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
                RequestContentEditorWindowClose?.Invoke(this, EventArgs.Empty);
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

            var url = $@"http://{settings.PostPageUrl(DbEntry)}";

            var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
        }
    }
}