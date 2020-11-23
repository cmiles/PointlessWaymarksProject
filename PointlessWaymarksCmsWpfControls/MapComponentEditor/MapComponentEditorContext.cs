#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.CommonHtml;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.GeoJsonList;
using PointlessWaymarksCmsWpfControls.LineList;
using PointlessWaymarksCmsWpfControls.PointList;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.StringDataEntry;
using PointlessWaymarksCmsWpfControls.UpdateNotesEditor;
using PointlessWaymarksCmsWpfControls.Utility;
using PointlessWaymarksCmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarksCmsWpfControls.Utility.ThreadSwitcher;

namespace PointlessWaymarksCmsWpfControls.MapComponentEditor
{
    public class MapComponentEditorContext : INotifyPropertyChanged
    {
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private List<MapComponentElement> _dbElements;
        private MapComponent _dbEntry;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private ObservableCollection<IContentCommonGuiListItem> _mapElements = new();

        private StatusControlContext _statusContext;
        private StringDataEntryContext _summaryEntry;
        private StringDataEntryContext _titleEntry;
        private UpdateNotesEditorContext _updateNotes;
        private string _userGeoContentInput;

        private MapComponentEditorContext(StatusControlContext statusContext)
        {
            _statusContext = statusContext;
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

        public List<MapComponentElement> DbElements
        {
            get => _dbElements;
            set
            {
                if (Equals(value, _dbElements)) return;
                _dbElements = value;
                OnPropertyChanged();
            }
        }

        public MapComponent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
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

        public ObservableCollection<IContentCommonGuiListItem> MapElements
        {
            get => _mapElements;
            set
            {
                if (Equals(value, _mapElements)) return;
                _mapElements = value;
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

        public StringDataEntryContext SummaryEntry
        {
            get => _summaryEntry;
            set
            {
                if (Equals(value, _summaryEntry)) return;
                _summaryEntry = value;
                OnPropertyChanged();
            }
        }

        public StringDataEntryContext TitleEntry
        {
            get => _titleEntry;
            set
            {
                if (Equals(value, _titleEntry)) return;
                _titleEntry = value;
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

        public string UserGeoContentInput
        {
            get => _userGeoContentInput;
            set
            {
                if (value == _userGeoContentInput) return;
                _userGeoContentInput = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async Task AddGeoJson(GeoJsonContent possibleGeoJson)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (MapElements.Any(x => x.ContentId() == possibleGeoJson.ContentId))
            {
                StatusContext.ToastWarning($"GeoJson {possibleGeoJson.Title} is already on the map");
                return;
            }

            //Todo initial list item
            MapElements.Add(new GeoJsonListListItem());
        }

        private async Task AddLine(LineContent possibleLine)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (MapElements.Any(x => x.ContentId() == possibleLine.ContentId))
            {
                StatusContext.ToastWarning($"Line {possibleLine.Title} is already on the map");
                return;
            }

            //Todo initial list item
            MapElements.Add(new LineListListItem());
        }

        private async Task AddPoint(PointContent possiblePoint)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (MapElements.Any(x => x.ContentId() == possiblePoint.ContentId))
            {
                StatusContext.ToastWarning($"GeoJson {possiblePoint.Title} is already on the map");
                return;
            }

            //Todo initial list item
            MapElements.Add(new PointListListItem());
        }

        public void CheckForChangesAndValidationIssues()
        {
            HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
            HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
        }

        public static async Task<MapComponentEditorContext> CreateInstance(StatusControlContext statusContext,
            MapComponent mapComponent)
        {
            var newControl = new MapComponentEditorContext(statusContext);
            await newControl.LoadData(mapComponent);
            return newControl;
        }

        public async Task LoadData(MapComponent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad;

            if (DbEntry.Id > 0)
            {
                var context = await Db.Context();
                DbElements = await context.MapComponentElements.Where(x => x.MapComponentContentId == DbEntry.ContentId)
                    .ToListAsync();
            }

            TitleEntry = StringDataEntryContext.CreateInstance();
            TitleEntry.Title = "Title";
            TitleEntry.HelpText = "Title Text";
            TitleEntry.ReferenceValue = DbEntry.Title.TrimNullToEmpty();
            TitleEntry.UserValue = DbEntry.Title.TrimNullToEmpty();
            TitleEntry.ValidationFunctions = new List<Func<string, (bool passed, string validationMessage)>>
            {
                CommonContentValidation.ValidateTitle
            };

            SummaryEntry = StringDataEntryContext.CreateInstance();
            SummaryEntry.Title = "Summary";
            SummaryEntry.HelpText = "A short text entry that will show in Search and short references to the content";
            SummaryEntry.ReferenceValue = DbEntry.Summary ?? string.Empty;
            SummaryEntry.UserValue = StringHelpers.NullToEmptyTrim(DbEntry.Summary);
            SummaryEntry.ValidationFunctions = new List<Func<string, (bool passed, string validationMessage)>>
            {
                CommonContentValidation.ValidateSummary
            };

            CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
            ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
            UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);

            PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidationIssues();
        }

        public async Task UserGeoContentIdInputToMap()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(UserGeoContentInput))
            {
                StatusContext.ToastError("Nothing to add?");
                return;
            }

            var codes = BracketCodeCommon.BracketCodeContentIds(UserGeoContentInput);

            if (codes == null || !codes.Any())
            {
                StatusContext.ToastError("No Content Ids found?");
                return;
            }

            var db = await Db.Context();

            foreach (var loopCode in codes)
            {
                var possiblePoint = await db.PointContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
                if (possiblePoint != null)
                {
                    await AddPoint(possiblePoint);
                    continue;
                }

                var possibleLine = await db.LineContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
                if (possibleLine != null)
                {
                    await AddLine(possibleLine);
                    continue;
                }

                var possibleGeoJson = await db.GeoJsonContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
                if (possibleGeoJson != null)
                {
                    await AddGeoJson(possibleGeoJson);
                    continue;
                }

                StatusContext.ToastWarning(
                    $"ContentId {loopCode} doesn't appear to be a valid point, line or GeoJson content for the map?");
            }
        }
    }
}