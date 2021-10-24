using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentInMainSiteFeed;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.GeoJsonContentEditor
{
    public class GeoJsonContentEditorContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues,
        ICheckForChangesAndValidation
    {
        private BodyContentEditorContext _bodyContent;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private GeoJsonContent _dbEntry;
        private Command _extractNewLinksCommand;
        private string _geoJsonText = string.Empty;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private HelpDisplayContext _helpContext;
        private Command _importGeoJsonFileCommand;
        private Command _importGeoJsonFromClipboardCommand;
        private Command _linkToClipboardCommand;
        private ContentInMainSiteFeedContext _mainSiteFeed;
        private string _previewGeoJsonDto;
        private string _previewHtml;
        private Command _refreshMapPreviewCommand;
        private Command _saveAndCloseCommand;
        private Command _saveCommand;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tagEdit;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;
        private Command _viewOnSiteCommand;
        public EventHandler RequestContentEditorWindowClose;

        private GeoJsonContentEditorContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            SaveCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(false));
            SaveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
            ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
                LinkExtraction.ExtractNewAndShowLinkContentEditors(
                    $"{BodyContent.BodyContent} {UpdateNotes.UpdateNotes}", StatusContext.ProgressTracker()));
            ImportGeoJsonFileCommand = StatusContext.RunBlockingTaskCommand(ImportGeoJsonFile);
            ImportGeoJsonFromClipboardCommand = StatusContext.RunBlockingTaskCommand(ImportGeoJsonFromClipboard);
            RefreshMapPreviewCommand = StatusContext.RunBlockingTaskCommand(RefreshMapPreview);
            LinkToClipboardCommand = StatusContext.RunBlockingTaskCommand(LinkToClipboard);

            HelpContext = new HelpDisplayContext(new List<string>
            {
                CommonFields.TitleSlugFolderSummary,
                BracketCodeHelpMarkdown.HelpBlock,
                GeoJsonContentHelpMarkdown.HelpBlock
            });

            PreviewHtml = WpfHtmlDocument.ToHtmlLeafletGeoJsonDocument("GeoJson",
                UserSettingsSingleton.CurrentSettings().LatitudeDefault,
                UserSettingsSingleton.CurrentSettings().LongitudeDefault, string.Empty);
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

        public GeoJsonContent DbEntry
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

        public string GeoJsonText
        {
            get => _geoJsonText;
            set
            {
                if (Equals(value, _geoJsonText)) return;
                _geoJsonText = value;
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

        public Command ImportGeoJsonFileCommand
        {
            get => _importGeoJsonFileCommand;
            set
            {
                if (Equals(value, _importGeoJsonFileCommand)) return;
                _importGeoJsonFileCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ImportGeoJsonFromClipboardCommand
        {
            get => _importGeoJsonFromClipboardCommand;
            set
            {
                if (Equals(value, _importGeoJsonFromClipboardCommand)) return;
                _importGeoJsonFromClipboardCommand = value;
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

        public ContentInMainSiteFeedContext MainSiteFeed
        {
            get => _mainSiteFeed;
            set
            {
                if (Equals(value, _mainSiteFeed)) return;
                _mainSiteFeed = value;
                OnPropertyChanged();
            }
        }

        public string PreviewGeoJsonDto
        {
            get => _previewGeoJsonDto;
            set
            {
                if (value == _previewGeoJsonDto) return;
                _previewGeoJsonDto = value;
                OnPropertyChanged();
            }
        }

        public string PreviewHtml
        {
            get => _previewHtml;
            set
            {
                if (value == _previewHtml) return;
                _previewHtml = value;
                OnPropertyChanged();
            }
        }

        public Command RefreshMapPreviewCommand
        {
            get => _refreshMapPreviewCommand;
            set
            {
                if (Equals(value, _refreshMapPreviewCommand)) return;
                _refreshMapPreviewCommand = value;
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

        public static async Task<GeoJsonContentEditorContext> CreateInstance(StatusControlContext statusContext,
            GeoJsonContent geoJsonContent)
        {
            var newControl = new GeoJsonContentEditorContext(statusContext);
            await newControl.LoadData(geoJsonContent);
            return newControl;
        }

        private GeoJsonContent CurrentStateToGeoJsonContent()
        {
            var newEntry = new GeoJsonContent();

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
            newEntry.FeedOn = MainSiteFeed.ShowInMainSiteFeedOnEntry.UserValue;
            newEntry.IsDraft = MainSiteFeed.ShowInMainSiteFeedEntry.UserValue;
            newEntry.Tags = TagEdit.TagListString();
            newEntry.Title = TitleSummarySlugFolder.TitleEntry.UserValue.TrimNullToEmpty();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedByEntry.UserValue.TrimNullToEmpty();
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes.TrimNullToEmpty();
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.BodyContent = BodyContent.BodyContent.TrimNullToEmpty();
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
            newEntry.GeoJson = GeoJsonText;

            return newEntry;
        }

        public async Task ImportGeoJsonFile()
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

            string geoJson;

            await using (var fs = new FileStream(newFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.Default))
            {
                geoJson = await sr.ReadToEndAsync();
            }

            var (isValid, explanation) = CommonContentValidation.GeoJsonValidation(geoJson);

            if (!isValid)
            {
                await StatusContext.ShowMessageWithOkButton("Error with GeoJson Import", explanation);
                return;
            }

            GeoJsonText = geoJson;
        }

        public async Task ImportGeoJsonFromClipboard()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Getting GeoJson from Clipboard");

            var clipboardText = Clipboard.GetText();

            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                StatusContext.ToastError("Blank/Empty Clipboard?");
                return;
            }

            var (isValid, explanation) = CommonContentValidation.GeoJsonValidation(clipboardText);

            if (!isValid)
            {
                await StatusContext.ShowMessageWithOkButton("Error with GeoJson Import", explanation);
                return;
            }

            GeoJsonText = clipboardText;
        }

        private async Task LinkToClipboard()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (DbEntry == null || DbEntry.Id < 1)
            {
                StatusContext.ToastError("Sorry - please save before getting link...");
                return;
            }

            var linkString = BracketCodeGeoJson.Create(DbEntry);

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(linkString);

            StatusContext.ToastSuccess($"To Clipboard: {linkString}");
        }

        public async Task LoadData(GeoJsonContent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var created = DateTime.Now;

            DbEntry = toLoad ?? new GeoJsonContent
            {
                BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                CreatedOn = created,
                FeedOn = created
            };

            TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry);
            CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
            MainSiteFeed = await ContentInMainSiteFeedContext.CreateInstance(StatusContext, DbEntry);
            ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
            UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
            TagEdit = TagsEditorContext.CreateInstance(StatusContext, DbEntry);
            BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);
            GeoJsonText = StringHelpers.NullToEmptyTrim(DbEntry?.GeoJson);

            PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidationIssues();

            if (propertyName == nameof(GeoJsonText)) StatusContext.RunNonBlockingTask(RefreshMapPreview);
        }

        public async Task RefreshMapPreview()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(GeoJsonText))
            {
                StatusContext.ToastError("Nothing to preview?");
                return;
            }

            //Using the new Guid as the page URL forces a changed value into the LineJsonDto
            PreviewGeoJsonDto = await GeoJsonData.GenerateGeoJson(GeoJsonText, Guid.NewGuid().ToString());
        }

        public async Task SaveAndGenerateHtml(bool closeAfterSave)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var (generationReturn, newContent) =
                await GeoJsonGenerator.SaveAndGenerateHtml(CurrentStateToGeoJsonContent(), null,
                    StatusContext.ProgressTracker());

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

            var url = $@"http://{settings.GeoJsonPageUrl(DbEntry)}";

            var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
        }
    }
}