#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using GongSolutions.Wpf.DragDrop;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Features;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.StringDataEntry;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using Point = NetTopologySuite.Geometries.Point;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor
{
    public class MapComponentEditorContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues,
        ICheckForChangesAndValidation, IDropTarget
    {
        private ContentIdViewerControlContext? _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext? _createdUpdatedDisplay;
        private List<MapElement> _dbElements = new();
        private MapComponent? _dbEntry;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private Command _linkToClipboardCommand;
        private ObservableCollection<IMapElementListItem>? _mapElements;
        private string _previewHtml;
        private string _previewMapJsonJsonDto = string.Empty;
        private Command _refreshMapPreviewCommand;
        private Command<IMapElementListItem> _removeItemCommand;
        private Command _saveAndCloseCommand;
        private Command _saveCommand;

        private StatusControlContext _statusContext;
        private StringDataEntryContext? _summaryEntry;
        private StringDataEntryContext? _titleEntry;
        private UpdateNotesEditorContext? _updateNotes;
        private Command _userGeoContentIdInputToMapCommand;
        private string _userGeoContentInput = string.Empty;

        public EventHandler? RequestContentEditorWindowClose;

        private MapComponentEditorContext(StatusControlContext statusContext)
        {
            _statusContext = statusContext;

            _userGeoContentIdInputToMapCommand = StatusContext.RunBlockingTaskCommand(UserGeoContentIdInputToMap);
            _saveCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(false));
            _saveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
            _refreshMapPreviewCommand = StatusContext.RunBlockingTaskCommand(RefreshMapPreview);
            _removeItemCommand = StatusContext.RunBlockingTaskCommand<IMapElementListItem>(RemoveItem);
            _linkToClipboardCommand = StatusContext.RunBlockingTaskCommand(LinkToClipboard);

            _previewHtml = WpfHtmlDocument.ToHtmlLeafletMapDocument("Map",
                UserSettingsSingleton.CurrentSettings().LatitudeDefault,
                UserSettingsSingleton.CurrentSettings().LongitudeDefault, string.Empty);
        }

        public ContentIdViewerControlContext? ContentId
        {
            get => _contentId;
            set
            {
                if (Equals(value, _contentId)) return;
                _contentId = value;
                OnPropertyChanged();
            }
        }

        public CreatedAndUpdatedByAndOnDisplayContext? CreatedUpdatedDisplay
        {
            get => _createdUpdatedDisplay;
            set
            {
                if (Equals(value, _createdUpdatedDisplay)) return;
                _createdUpdatedDisplay = value;
                OnPropertyChanged();
            }
        }

        public List<MapElement> DbElements
        {
            get => _dbElements;
            set
            {
                if (Equals(value, _dbElements)) return;
                _dbElements = value;
                OnPropertyChanged();
            }
        }

        public MapComponent? DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
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

        public ObservableCollection<IMapElementListItem>? MapElements
        {
            get => _mapElements;
            set
            {
                if (Equals(value, _mapElements)) return;
                _mapElements = value;
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

        public string PreviewMapJsonDto
        {
            get => _previewMapJsonJsonDto;
            set
            {
                if (Equals(value, _previewMapJsonJsonDto)) return;
                _previewMapJsonJsonDto = value;
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

        public Command<IMapElementListItem> RemoveItemCommand
        {
            get => _removeItemCommand;
            set
            {
                if (Equals(value, _removeItemCommand)) return;
                _removeItemCommand = value;
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

        public StringDataEntryContext? SummaryEntry
        {
            get => _summaryEntry;
            set
            {
                if (Equals(value, _summaryEntry)) return;
                _summaryEntry = value;
                OnPropertyChanged();
            }
        }

        public StringDataEntryContext? TitleEntry
        {
            get => _titleEntry;
            set
            {
                if (Equals(value, _titleEntry)) return;
                _titleEntry = value;
                OnPropertyChanged();
            }
        }

        public UpdateNotesEditorContext? UpdateNotes
        {
            get => _updateNotes;
            set
            {
                if (Equals(value, _updateNotes)) return;
                _updateNotes = value;
                OnPropertyChanged();
            }
        }

        public Command UserGeoContentIdInputToMapCommand
        {
            get => _userGeoContentIdInputToMapCommand;
            set
            {
                if (Equals(value, _userGeoContentIdInputToMapCommand)) return;
                _userGeoContentIdInputToMapCommand = value;
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

        public void CheckForChangesAndValidationIssues()
        {
            HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
            HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
        }

        public void DragOver(IDropInfo dropInfo)
        {
            var data = dropInfo.Data;
            if (data is string) dropInfo.Effects = DragDropEffects.Copy;
            if (data is not DataObject dataObject) return;
            if (dataObject.ContainsText()) dropInfo.Effects = DragDropEffects.Copy;
        }

        public async void Drop(IDropInfo dropInfo)
        {
            var textToProcess = string.Empty;

            var data = dropInfo.Data;
            textToProcess = data switch
            {
                string stringData => stringData,
                DataObject dataObject when dataObject.ContainsText() => dataObject.GetText(),
                _ => textToProcess
            };

            var contentIds = BracketCodeCommon.BracketCodeContentIds(textToProcess);
            foreach (var loopGuids in contentIds) await TryAddSpatialType(loopGuids);
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

        public event PropertyChangedEventHandler? PropertyChanged;

        private async Task AddGeoJson(GeoJsonContent possibleGeoJson, MapElement? loopContent = null,
            bool guiNotificationAndMapRefreshWhenAdded = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (MapElements == null) return;

            if (MapElements.Any(x => x.ContentId() == possibleGeoJson.ContentId))
            {
                StatusContext.ToastWarning($"GeoJson {possibleGeoJson.Title} is already on the map");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            MapElements.Add(new MapElementListGeoJsonItem
            {
                DbEntry = possibleGeoJson,
                SmallImageUrl = ContentListContext.GetSmallImageUrl(possibleGeoJson),
                InInitialView = loopContent?.IncludeInDefaultView ?? true,
                ShowInitialDetails = loopContent?.ShowDetailsDefault ?? false
            });

            if (guiNotificationAndMapRefreshWhenAdded)
            {
                StatusContext.ToastSuccess($"Added Point - {possibleGeoJson.Title}");
                await RefreshMapPreview();
            }
        }

        private async Task AddLine(LineContent possibleLine, MapElement? loopContent = null,
            bool guiNotificationAndMapRefreshWhenAdded = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (MapElements == null) return;

            if (MapElements.Any(x => x.ContentId() == possibleLine.ContentId))
            {
                StatusContext.ToastWarning($"Line {possibleLine.Title} is already on the map");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            MapElements.Add(new MapElementListLineItem
            {
                DbEntry = possibleLine,
                SmallImageUrl = ContentListContext.GetSmallImageUrl(possibleLine),
                InInitialView = loopContent?.IncludeInDefaultView ?? true,
                ShowInitialDetails = loopContent?.ShowDetailsDefault ?? false
            });

            if (guiNotificationAndMapRefreshWhenAdded)
            {
                StatusContext.ToastSuccess($"Added Point - {possibleLine.Title}");
                await RefreshMapPreview();
            }
        }

        private async Task AddPoint(PointContentDto possiblePoint, MapElement? loopContent = null,
            bool guiNotificationAndMapRefreshWhenAdded = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (MapElements == null) return;

            if (MapElements.Any(x => x.ContentId() == possiblePoint.ContentId))
            {
                StatusContext.ToastWarning($"Point {possiblePoint.Title} is already on the map");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            MapElements.Add(new MapElementListPointItem
            {
                DbEntry = possiblePoint,
                SmallImageUrl = ContentListContext.GetSmallImageUrl(possiblePoint),
                InInitialView = loopContent?.IncludeInDefaultView ?? true,
                ShowInitialDetails = loopContent?.ShowDetailsDefault ?? false
            });

            if (guiNotificationAndMapRefreshWhenAdded)
            {
                StatusContext.ToastSuccess($"Added Point - {possiblePoint.Title}");
                await RefreshMapPreview();
            }
        }

        public static async Task<MapComponentEditorContext> CreateInstance(StatusControlContext statusContext,
            MapComponent mapComponent)
        {
            var newControl = new MapComponentEditorContext(statusContext);
            await newControl.LoadData(mapComponent);
            return newControl;
        }

        public MapComponentDto CurrentStateToContent()
        {
            var newEntry = new MapComponent();

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
                newEntry.LastUpdatedBy =
                    CreatedUpdatedDisplay?.UpdatedByEntry.UserValue.TrimNullToEmpty() ?? string.Empty;
            }

            newEntry.Summary = SummaryEntry?.UserValue.TrimNullToEmpty() ?? string.Empty;
            newEntry.Title = TitleEntry?.UserValue.TrimNullToEmpty() ?? string.Empty;
            newEntry.CreatedBy = CreatedUpdatedDisplay?.CreatedByEntry.UserValue.TrimNullToEmpty() ?? string.Empty;
            newEntry.UpdateNotes = UpdateNotes?.UpdateNotes.TrimNullToEmpty();
            newEntry.UpdateNotesFormat = UpdateNotes?.UpdateNotesFormat.SelectedContentFormatAsString ?? string.Empty;

            var currentElementList = MapElements?.ToList() ?? new List<IMapElementListItem>();
            var finalElementList = currentElementList.Select(x => new MapElement
            {
                MapComponentContentId = newEntry.ContentId,
                ElementContentId = x.ContentId() ?? Guid.Empty,
                IsFeaturedElement = x.IsFeaturedElement,
                IncludeInDefaultView = x.InInitialView,
                ShowDetailsDefault = x.ShowInitialDetails
            }).ToList();

            return new MapComponentDto(newEntry, finalElementList);
        }

        private async Task LinkToClipboard()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (DbEntry == null || DbEntry.Id < 1)
            {
                StatusContext.ToastError("Sorry - please save before getting link...");
                return;
            }

            var linkString = BracketCodeMapComponents.Create(DbEntry);

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(linkString);

            StatusContext.ToastSuccess($"To Clipboard: {linkString}");
        }

        public async Task LoadData(MapComponent? toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new MapComponent();

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
            TitleEntry.ValidationFunctions = new List<Func<string, IsValid>> {CommonContentValidation.ValidateTitle};

            SummaryEntry = StringDataEntryContext.CreateInstance();
            SummaryEntry.Title = "Summary";
            SummaryEntry.HelpText = "A short text entry that will show in Search and short references to the content";
            SummaryEntry.ReferenceValue = DbEntry.Summary ?? string.Empty;
            SummaryEntry.UserValue = StringHelpers.NullToEmptyTrim(DbEntry.Summary);
            SummaryEntry.ValidationFunctions = new List<Func<string, IsValid>>
            {
                CommonContentValidation.ValidateSummary
            };

            CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
            ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
            UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);

            PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);

            var db = await Db.Context();

            await ThreadSwitcher.ResumeForegroundAsync();

            MapElements ??= new ObservableCollection<IMapElementListItem>();

            MapElements.Clear();

            await ThreadSwitcher.ResumeBackgroundAsync();

            foreach (var loopContent in DbElements)
            {
                var elementContent = await db.ContentFromContentId(loopContent.ElementContentId);

                switch (elementContent)
                {
                    case GeoJsonContent gj:
                        await AddGeoJson(gj, loopContent);
                        break;
                    case LineContent l:
                        await AddLine(l, loopContent);
                        break;
                    case PointContentDto p:
                        await AddPoint(p, loopContent);
                        break;
                }
            }

            if (MapElements.Any()) await RefreshMapPreview();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidationIssues();
        }

        public async Task RefreshMapPreview()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (MapElements == null || !MapElements.Any())
            {
                PreviewMapJsonDto = await SpatialHelpers.SerializeAsGeoJson(new MapJsonDto(Guid.NewGuid(),
                    new GeoJsonData.SpatialBounds(0, 0, 0, 0), new List<FeatureCollection>()));
                return;
            }

            var geoJsonList = new List<FeatureCollection>();

            var boundsKeeper = new List<Point>();

            foreach (var loopElements in MapElements)
            {
                if (loopElements is MapElementListGeoJsonItem {DbEntry: {GeoJson: { }}} mapGeoJson)
                {
                    geoJsonList.Add(SpatialConverters.GeoJsonToFeatureCollection(mapGeoJson.DbEntry.GeoJson));
                    boundsKeeper.Add(new Point(mapGeoJson.DbEntry.InitialViewBoundsMaxLongitude,
                        mapGeoJson.DbEntry.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapGeoJson.DbEntry.InitialViewBoundsMinLongitude,
                        mapGeoJson.DbEntry.InitialViewBoundsMinLatitude));
                }

                if (loopElements is MapElementListLineItem {DbEntry: {Line: { }}} mapLine)
                {
                    geoJsonList.Add(SpatialConverters.GeoJsonToFeatureCollection(mapLine.DbEntry.Line));
                    boundsKeeper.Add(new Point(mapLine.DbEntry.InitialViewBoundsMaxLongitude,
                        mapLine.DbEntry.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapLine.DbEntry.InitialViewBoundsMinLongitude,
                        mapLine.DbEntry.InitialViewBoundsMinLatitude));
                }
            }

            if (MapElements.Any(x => x is MapElementListPointItem))
            {
                var featureCollection = new FeatureCollection();

                foreach (var loopElements in MapElements.Where(x => x is MapElementListPointItem)
                    .Cast<MapElementListPointItem>().Where(x => x.DbEntry != null).ToList())
                {
                    if (loopElements.DbEntry == null) continue;

                    featureCollection.Add(new Feature(
                        SpatialHelpers.Wgs84Point(loopElements.DbEntry.Longitude, loopElements.DbEntry.Latitude,
                            loopElements.DbEntry.Elevation ?? 0), new AttributesTable()));
                    boundsKeeper.Add(new Point(loopElements.DbEntry.Longitude, loopElements.DbEntry.Latitude));
                }

                geoJsonList.Add(featureCollection);
            }

            var bounds = SpatialConverters.PointBoundingBox(boundsKeeper);

            var dto = new MapJsonDto(Guid.NewGuid(),
                new GeoJsonData.SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), geoJsonList);

            //Using the new Guid as the page URL forces a changed value into the LineJsonDto
            PreviewMapJsonDto = await SpatialHelpers.SerializeAsGeoJson(dto);
        }

        private async Task RemoveItem(IMapElementListItem toRemove)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            try
            {
                MapElements?.Remove(toRemove);
            }
            catch (Exception e)
            {
                StatusContext.ToastError($"Trouble Removing Map Element - {e.Message}");
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            await RefreshMapPreview();
        }

        public async Task SaveAndGenerateHtml(bool closeAfterSave)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var (generationReturn, newContent) =
                await MapComponentGenerator.SaveAndGenerateData(CurrentStateToContent(), null,
                    StatusContext.ProgressTracker());

            if (generationReturn.HasError || newContent == null)
            {
                await StatusContext.ShowMessageWithOkButton("Problem Saving and Generating Html",
                    generationReturn.GenerationNote);
                return;
            }

            await LoadData(newContent.Map);

            if (closeAfterSave)
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                RequestContentEditorWindowClose?.Invoke(this, new EventArgs());
            }
        }

        private async Task TryAddSpatialType(Guid toAdd)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            if (db.PointContents.Any(x => x.ContentId == toAdd))
            {
                await AddPoint((await Db.PointAndPointDetails(toAdd))!, guiNotificationAndMapRefreshWhenAdded: true)
                    .ConfigureAwait(false);
                return;
            }

            if (db.GeoJsonContents.Any(x => x.ContentId == toAdd))
            {
                await AddGeoJson(await db.GeoJsonContents.SingleAsync(x => x.ContentId == toAdd),
                    guiNotificationAndMapRefreshWhenAdded: true).ConfigureAwait(false);
                return;
            }

            if (db.LineContents.Any(x => x.ContentId == toAdd))
            {
                await AddLine(await db.LineContents.SingleAsync(x => x.ContentId == toAdd),
                    guiNotificationAndMapRefreshWhenAdded: true).ConfigureAwait(false);
                return;
            }

            StatusContext.ToastError("Item isn't a spatial type or isn't in the db?");
        }

        public async Task UserGeoContentIdInputToMap()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(UserGeoContentInput))
            {
                StatusContext.ToastError("Nothing to add?");
                return;
            }

            var codes = new List<Guid>();

            var regexObj = new Regex(@"(?:(\()|(\{))?\b[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\b(?(1)\))(?(2)\})",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            Match matchResult = regexObj.Match(UserGeoContentInput);
            while (matchResult.Success)
            {
                codes.Add(Guid.Parse(matchResult.Value));
                matchResult = matchResult.NextMatch();
            }

            codes.AddRange(BracketCodeCommon.BracketCodeContentIds(UserGeoContentInput));

            codes = codes.Distinct().ToList();

            if (!codes.Any())
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
                    var pointDto = (await Db.PointAndPointDetails(possiblePoint.ContentId, db))!;
                    await AddPoint(pointDto);
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

            UserGeoContentInput = string.Empty;
        }

        public record MapJsonDto(Guid Identifier, GeoJsonData.SpatialBounds Bounds,
            List<FeatureCollection> GeoJsonLayers);
    }
}