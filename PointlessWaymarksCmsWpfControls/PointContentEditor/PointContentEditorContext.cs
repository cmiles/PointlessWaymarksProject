using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Spatial;
using PointlessWaymarksCmsData.Spatial.Elevation;
using PointlessWaymarksCmsWpfControls.BodyContentEditor;
using PointlessWaymarksCmsWpfControls.BoolDataEntry;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.ConversionDataEntry;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.HelpDisplay;
using PointlessWaymarksCmsWpfControls.PointDetailEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarksCmsWpfControls.UpdateNotesEditor;
using PointlessWaymarksCmsWpfControls.Utility;
using PointlessWaymarksCmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarksCmsWpfControls.Utility.ThreadSwitcher;

namespace PointlessWaymarksCmsWpfControls.PointContentEditor
{
    public class PointContentEditorContext : INotifyPropertyChanged, IHasChanges, ICheckForChangesAndValidation,
        IHasValidationIssues
    {
        private BodyContentEditorContext _bodyContent;
        private bool _broadcastLatLongChange = true;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private PointContent _dbEntry;
        private ConversionDataEntryContext<double?> _elevationEntry;
        private Command _extractNewLinksCommand;
        private Command _getElevationCommand;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private HelpDisplayContext _helpContext;
        private ConversionDataEntryContext<double> _latitudeEntry;
        private ConversionDataEntryContext<double> _longitudeEntry;
        private PointDetailListContext _pointDetails;
        private Command _saveAndCloseCommand;
        private Command _saveCommand;
        private BoolDataEntryContext _showInSiteFeed;
        private TagsEditorContext _tagEdit;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;
        private Command _viewOnSiteCommand;

        public EventHandler RequestContentEditorWindowClose;

        private PointContentEditorContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            HelpContext =
                new HelpDisplayContext(CommonFields.TitleSlugFolderSummary + BracketCodeHelpMarkdown.HelpBlock);

            SaveCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(false));
            SaveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
            ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
                LinkExtraction.ExtractNewAndShowLinkContentEditors(
                    $"{BodyContent.BodyContent} {UpdateNotes.UpdateNotes}", StatusContext.ProgressTracker()));
            GetElevationCommand = StatusContext.RunBlockingTaskCommand(GetElevation);
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

        public ConversionDataEntryContext<double?> ElevationEntry
        {
            get => _elevationEntry;
            set
            {
                if (Equals(value, _elevationEntry)) return;
                _elevationEntry = value;
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

        public Command GetElevationCommand
        {
            get => _getElevationCommand;
            set
            {
                if (Equals(value, _getElevationCommand)) return;
                _getElevationCommand = value;
                OnPropertyChanged();
            }
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

        public ConversionDataEntryContext<double> LatitudeEntry
        {
            get => _latitudeEntry;
            set
            {
                if (Equals(value, _latitudeEntry)) return;
                _latitudeEntry = value;
                OnPropertyChanged();
            }
        }

        public ConversionDataEntryContext<double> LongitudeEntry
        {
            get => _longitudeEntry;
            set
            {
                if (Equals(value, _longitudeEntry)) return;
                _longitudeEntry = value;
                OnPropertyChanged();
            }
        }

        public PointDetailListContext PointDetails
        {
            get => _pointDetails;
            set
            {
                if (Equals(value, _pointDetails)) return;
                _pointDetails = value;
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

        public event PropertyChangedEventHandler PropertyChanged;

        public static async Task<PointContentEditorContext> CreateInstance(StatusControlContext statusContext,
            PointContent pointContent)
        {
            var newControl = new PointContentEditorContext(statusContext);
            await newControl.LoadData(pointContent);
            return newControl;
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
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedByEntry.UserValue.TrimNullToEmpty();
            }

            newEntry.Folder = TitleSummarySlugFolder.FolderEntry.UserValue.TrimNullToEmpty();
            newEntry.Slug = TitleSummarySlugFolder.SlugEntry.UserValue.TrimNullToEmpty();
            newEntry.Summary = TitleSummarySlugFolder.SummaryEntry.UserValue.TrimNullToEmpty();
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.UserValue;
            newEntry.Tags = TagEdit.TagListString();
            newEntry.Title = TitleSummarySlugFolder.TitleEntry.UserValue.TrimNullToEmpty();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedByEntry.UserValue.TrimNullToEmpty();
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes.TrimNullToEmpty();
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.BodyContent = BodyContent.BodyContent.TrimNullToEmpty();
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;

            newEntry.Latitude = LatitudeEntry.UserValue;
            newEntry.Longitude = LongitudeEntry.UserValue;
            newEntry.Elevation = ElevationEntry.UserValue;

            return newEntry;
        }

        private PointContentDto CurrentStateToPointContentDto()
        {
            var toReturn = new PointContentDto();
            var currentPoint = CurrentStateToPointContent();
            toReturn.InjectFrom(currentPoint);
            toReturn.PointDetails = PointDetails.CurrentStateToPointDetailsList() ?? new List<PointDetail>();
            return toReturn;
        }


        public async Task GetElevation()
        {
            if (LatitudeEntry.HasValidationIssues || LongitudeEntry.HasValidationIssues)
            {
                StatusContext.ToastError("Lat Long is not valid");
                return;
            }

            var httpClient = new HttpClient();

            try
            {
                var elevationResult = await ElevationService.OpenTopoNedElevation(httpClient, LatitudeEntry.UserValue,
                    LongitudeEntry.UserValue, StatusContext.ProgressTracker());

                if (elevationResult != null)
                {
                    ElevationEntry.UserText = elevationResult.MetersToFeet().ToString("F0");

                    StatusContext.ToastSuccess(
                        $"Set elevation of {ElevationEntry.UserValue} from Open Topo Data - www.opentopodata.org - NED data set");

                    return;
                }
            }
            catch (Exception e)
            {
                await EventLogContext.TryWriteExceptionToLog(e, StatusContext.StatusControlContextId.ToString(),
                    $"Open Topo Data NED Request for {LatitudeEntry.UserValue}, {LongitudeEntry.UserValue}");
            }

            try
            {
                var elevationResult = await ElevationService.OpenTopoMapZenElevation(httpClient,
                    LatitudeEntry.UserValue, LongitudeEntry.UserValue, StatusContext.ProgressTracker());

                if (elevationResult == null)
                {
                    await EventLogContext.TryWriteDiagnosticMessageToLog(
                        "Unexpected Null return from an Open Topo Data Mapzen Request to {LatitudeEntry.UserValue}, {LongitudeEntry.UserValue}",
                        StatusContext.StatusControlContextId.ToString());
                    StatusContext.ToastError("Elevation Exception - unexpected Null return...");
                    return;
                }

                ElevationEntry.UserText = elevationResult.MetersToFeet().ToString("F0");

                StatusContext.ToastSuccess(
                    $"Set elevation of {ElevationEntry.UserValue} from Open Topo Data - www.opentopodata.org - Mapzen data set");
            }
            catch (Exception e)
            {
                await EventLogContext.TryWriteExceptionToLog(e, StatusContext.StatusControlContextId.ToString(),
                    $"Open Topo Data Mapzen Request for {LatitudeEntry.UserValue}, {LongitudeEntry.UserValue}");
                StatusContext.ToastError($"Elevation Exception - {e.Message}");
            }
        }

        private void LatitudeLongitudeChangeBroadcast()
        {
            if (_broadcastLatLongChange && !LatitudeEntry.HasValidationIssues && !LongitudeEntry.HasValidationIssues)
                RaisePointLatitudeLongitudeChange?.Invoke(this,
                    new PointLatitudeLongitudeChange(LatitudeEntry.UserValue, LongitudeEntry.UserValue));
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

            TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry);
            CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
            ShowInSiteFeed = BoolDataEntryContext.CreateInstanceForShowInSiteFeed(DbEntry, false);
            ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
            UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
            TagEdit = TagsEditorContext.CreateInstance(StatusContext, DbEntry);
            BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);

            ElevationEntry = ConversionDataEntryContext<double?>.CreateInstance();
            ElevationEntry.Converter = ConversionDataEntryHelpers.DoubleNullableConversion;
            ElevationEntry.ValidationFunctions = new List<Func<double?, (bool passed, string validationMessage)>>
            {
                CommonContentValidation.ElevationValidation
            };
            ElevationEntry.Title = "Elevation";
            ElevationEntry.HelpText = "Elevation in Feet";
            ElevationEntry.ReferenceValue = DbEntry.Elevation;
            ElevationEntry.UserText = DbEntry.Elevation?.ToString("F0") ?? string.Empty;

            LatitudeEntry = ConversionDataEntryContext<double>.CreateInstance();
            LatitudeEntry.Converter = ConversionDataEntryHelpers.DoubleConversion;
            LatitudeEntry.ValidationFunctions = new List<Func<double, (bool passed, string validationMessage)>>
            {
                CommonContentValidation.LatitudeValidation
            };
            LatitudeEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .000001);
            LatitudeEntry.Title = "Latitude";
            LatitudeEntry.HelpText = "In DDD.DDDDDD°";
            LatitudeEntry.ReferenceValue = DbEntry.Latitude;
            LatitudeEntry.UserText = DbEntry.Latitude.ToString("F6");
            LatitudeEntry.PropertyChanged += (_, args) =>
            {
                if (string.IsNullOrWhiteSpace(args.PropertyName)) return;
                if (args.PropertyName == nameof(LatitudeEntry.UserValue)) LatitudeLongitudeChangeBroadcast();
            };

            LongitudeEntry = ConversionDataEntryContext<double>.CreateInstance();
            LongitudeEntry.Converter = ConversionDataEntryHelpers.DoubleConversion;
            LongitudeEntry.ValidationFunctions = new List<Func<double, (bool passed, string validationMessage)>>
            {
                CommonContentValidation.LongitudeValidation
            };
            LongitudeEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .000001);
            LongitudeEntry.Title = "Longitude";
            LongitudeEntry.HelpText = "In DDD.DDDDDD°";
            LongitudeEntry.ReferenceValue = DbEntry.Longitude;
            LongitudeEntry.UserText = DbEntry.Longitude.ToString("F6");
            LongitudeEntry.PropertyChanged += (_, args) =>
            {
                if (string.IsNullOrWhiteSpace(args.PropertyName)) return;
                if (args.PropertyName == nameof(LongitudeEntry.UserValue)) LatitudeLongitudeChangeBroadcast();
            };

            PointDetails = await PointDetailListContext.CreateInstance(StatusContext, DbEntry);

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

        public void OnRaisePointLatitudeLongitudeChange(object sender, PointLatitudeLongitudeChange e)
        {
            _broadcastLatLongChange = false;

            LatitudeEntry.UserText = e.Latitude.ToString("F6");
            LongitudeEntry.UserText = e.Longitude.ToString("F6");

            _broadcastLatLongChange = true;
        }

        public event EventHandler<PointLatitudeLongitudeChange> RaisePointLatitudeLongitudeChange;

        public async Task SaveAndGenerateHtml(bool closeAfterSave)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var (generationReturn, newContent) =
                await PointGenerator.SaveAndGenerateHtml(CurrentStateToPointContentDto(), null,
                    StatusContext.ProgressTracker());

            if (generationReturn.HasError || newContent == null)
            {
                await StatusContext.ShowMessageWithOkButton("Problem Saving and Generating Html",
                    generationReturn.GenerationNote);
                return;
            }

            await LoadData(Db.PointContentDtoToPointContentAndDetails(newContent).content);

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

            var url = $@"http://{settings.PointPageUrl(DbEntry)}";

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }
    }
}