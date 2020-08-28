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
using PointlessWaymarksCmsData.Spatial;
using PointlessWaymarksCmsWpfControls.BodyContentEditor;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.HelpDisplay;
using PointlessWaymarksCmsWpfControls.ShowInMainSiteFeedEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarksCmsWpfControls.UpdateNotesEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.PointContentEditor
{
    public class PointContentEditorContext : INotifyPropertyChanged, IHasUnsavedChanges
    {
        private BodyContentEditorContext _bodyContent;
        private bool _broadcastLatLongChange = true;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private PointContent _dbEntry;
        private double _elevation;
        private bool _elevationHasChanges;
        private bool _elevationHasValidationIssues;
        private string _elevationValidationMessage;
        private Command _extractNewLinksCommand;
        private HelpDisplayContext _helpContext;
        private double _latitude;
        private bool _latitudeHasChanges;
        private bool _latitudeHasValidationIssues;
        private string _latitudeValidationMessage;
        private double _longitude;
        private bool _longitudeHasChanges;
        private bool _longitudeHasValidationIssues;
        private string _longitudeValidationMessage;
        private Command _saveAndGenerateHtmlCommand;
        private ShowInMainSiteFeedEditorContext _showInSiteFeed;
        private TagsEditorContext _tagEdit;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;
        private Command _viewOnSiteCommand;

        public PointContentEditorContext(StatusControlContext statusContext, PointContent pointContent)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            HelpContext =
                new HelpDisplayContext(CommonFields.TitleSlugFolderSummary + BracketCodeHelpMarkdown.HelpBlock);

            SaveAndGenerateHtmlCommand = StatusContext.RunBlockingTaskCommand(SaveAndGenerateHtml);
            ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
                LinkExtraction.ExtractNewAndShowLinkContentEditors(
                    $"{BodyContent.BodyContent} {UpdateNotes.UpdateNotes}", StatusContext.ProgressTracker()));


            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(pointContent));
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

        public PointContent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public double Elevation
        {
            get => _elevation;
            set
            {
                if (value.Equals(_elevation)) return;
                _elevation = value;
                OnPropertyChanged();
            }
        }

        public bool ElevationHasChanges
        {
            get => _elevationHasChanges;
            set
            {
                if (value == _elevationHasChanges) return;
                _elevationHasChanges = value;
                OnPropertyChanged();
            }
        }

        public bool ElevationHasValidationIssues
        {
            get => _elevationHasValidationIssues;
            set
            {
                if (value == _elevationHasValidationIssues) return;
                _elevationHasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public string ElevationValidationMessage
        {
            get => _elevationValidationMessage;
            set
            {
                if (value == _elevationValidationMessage) return;
                _elevationValidationMessage = value;
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

        public double Latitude
        {
            get => _latitude;
            set
            {
                if (value.Equals(_latitude)) return;
                _latitude = value;
                OnPropertyChanged();
            }
        }

        public bool LatitudeHasChanges
        {
            get => _latitudeHasChanges;
            set
            {
                if (value == _latitudeHasChanges) return;
                _latitudeHasChanges = value;
                OnPropertyChanged();
            }
        }

        public bool LatitudeHasValidationIssues
        {
            get => _latitudeHasValidationIssues;
            set
            {
                if (value == _latitudeHasValidationIssues) return;
                _latitudeHasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public string LatitudeValidationMessage
        {
            get => _latitudeValidationMessage;
            set
            {
                if (value == _latitudeValidationMessage) return;
                _latitudeValidationMessage = value;
                OnPropertyChanged();
            }
        }

        public double Longitude
        {
            get => _longitude;
            set
            {
                if (value.Equals(_longitude)) return;
                _longitude = value;
                OnPropertyChanged();
            }
        }

        public bool LongitudeHasChanges
        {
            get => _longitudeHasChanges;
            set
            {
                if (value == _longitudeHasChanges) return;
                _longitudeHasChanges = value;
                OnPropertyChanged();
            }
        }

        public bool LongitudeHasValidationIssues
        {
            get => _longitudeHasValidationIssues;
            set
            {
                if (value == _longitudeHasValidationIssues) return;
                _longitudeHasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public string LongitudeValidationMessage
        {
            get => _longitudeValidationMessage;
            set
            {
                if (value == _longitudeValidationMessage) return;
                _longitudeValidationMessage = value;
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

        public bool HasChanges()
        {
            return TitleSummarySlugFolder.HasChanges || CreatedUpdatedDisplay.HasChanges ||
                   ShowInSiteFeed.ShowInMainSiteHasChanges || BodyContent.HasChanges || UpdateNotes.HasChanges ||
                   TagEdit.TagsHaveChanges || LongitudeHasChanges || LatitudeHasChanges || ElevationHasChanges;
        }

        public event PropertyChangedEventHandler PropertyChanged;


        private void CheckForChangesAndValidate(bool latitudeLongitudeHasChanges)
        {
            SpatialHelpers.RoundLatLongElevationToSixPlaces(this);

            LatitudeHasChanges = DbEntry?.Latitude.IsApproximatelyEqualTo(Latitude, .000001) ?? true;

            LongitudeHasChanges = DbEntry?.Longitude.IsApproximatelyEqualTo(Longitude, .000001) ?? true;

            ElevationHasChanges = DbEntry?.Elevation.IsApproximatelyEqualTo(Elevation, .000001) ?? true;

            var latitudeValidationResult = CommonContentValidation.LatitudeValidation(Latitude);
            LatitudeHasValidationIssues = !latitudeValidationResult.isValid;
            LatitudeValidationMessage = latitudeValidationResult.explanation;

            var longitudeValidationResult = CommonContentValidation.LongitudeValidation(Longitude);
            LongitudeHasValidationIssues = !longitudeValidationResult.isValid;
            LongitudeValidationMessage = longitudeValidationResult.explanation;

            if (_broadcastLatLongChange && latitudeLongitudeHasChanges && !LatitudeHasValidationIssues &&
                !LongitudeHasValidationIssues)
                RaisePointLatitudeLongitudeChange?.Invoke(this, new PointLatitudeLongitudeChange(Latitude, Longitude));

            var elevationValidationResult = CommonContentValidation.ElevationValidation(Elevation);
            ElevationHasValidationIssues = !elevationValidationResult.isValid;
            ElevationValidationMessage = elevationValidationResult.explanation;
        }

        private PointContent CurrentStateToPointContent()
        {
            var newEntry = new PointContent();

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
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedBy.TrimNullToEmpty();
            }

            newEntry.Folder = TitleSummarySlugFolder.Folder.TrimNullToEmpty();
            newEntry.Slug = TitleSummarySlugFolder.Slug.TrimNullToEmpty();
            newEntry.Summary = TitleSummarySlugFolder.Summary.TrimNullToEmpty();
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.ShowInMainSite;
            newEntry.Tags = TagEdit.TagListString();
            newEntry.Title = TitleSummarySlugFolder.Title.TrimNullToEmpty();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedBy.TrimNullToEmpty();
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes.TrimNullToEmpty();
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.BodyContent = BodyContent.BodyContent.TrimNullToEmpty();
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;

            newEntry.Latitude = Latitude;
            newEntry.Longitude = Longitude;
            newEntry.Elevation = Elevation;

            return newEntry;
        }

        public async Task LoadData(PointContent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new PointContent
            {
                BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
                ShowInMainSiteFeed = true,
                Latitude = UserSettingsSingleton.CurrentSettings().LatitudeDefault,
                Longitude = UserSettingsSingleton.CurrentSettings().LongitudeDefault
            };

            TitleSummarySlugFolder = new TitleSummarySlugEditorContext(StatusContext, DbEntry,
                UserSettingsSingleton.CurrentSettings().LocalSitePointDirectory());
            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, DbEntry);
            ShowInSiteFeed = new ShowInMainSiteFeedEditorContext(StatusContext, DbEntry, true);
            ContentId = new ContentIdViewerControlContext(StatusContext, DbEntry);
            UpdateNotes = new UpdateNotesEditorContext(StatusContext, DbEntry);
            TagEdit = new TagsEditorContext(StatusContext, DbEntry);
            BodyContent = new BodyContentEditorContext(StatusContext, DbEntry);
            Latitude = DbEntry.Latitude;
            Longitude = DbEntry.Longitude;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidate(propertyName == "Latitude" || propertyName == "Longitude");
        }

        public void OnRaisePointLatitudeLongitudeChange(object sender, PointLatitudeLongitudeChange e)
        {
            _broadcastLatLongChange = false;

            Latitude = e.Latitude;
            Longitude = e.Longitude;

            _broadcastLatLongChange = true;
        }

        public event EventHandler<PointLatitudeLongitudeChange> RaisePointLatitudeLongitudeChange;

        public async Task SaveAndGenerateHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var (generationReturn, newContent) = await PointGenerator.SaveAndGenerateHtml(CurrentStateToPointContent(),
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

            var url = $@"http://{settings.PointPageUrl(DbEntry)}";

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }
    }
}