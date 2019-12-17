using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using Omu.ValueInjecter;
using TheLemmonWorkshopData;
using TheLemmonWorkshopData.Elevation;
using TheLemmonWorkshopData.Models;
using TheLemmonWorkshopWpfControls.ContentFormat;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.GeoDataPicker;
using TheLemmonWorkshopWpfControls.MainImageFormat;
using TheLemmonWorkshopWpfControls.Models;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.ItemContentEditor
{
    public class ItemContentEditorContext : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private ContentFormatChooserContext _bodyContentFormatContext;
        private MainImageFormatChooserContext _mainImageFormatContext;
        private StatusControlContext _statusContext;
        private ContentFormatChooserContext _updateNotesFormatContext;
        private UserSiteContent _userContent;

        public ItemContentEditorContext()
        {
            StatusContext = new StatusControlContext();
            GeoDataPickerContext = new GeoDataPickerContext(StatusContext);

            SaveContentCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SaveContent));

            SelectGeoDataCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SelectGeoData));
            UpdateSelectedGeoDataElevation =
                new RelayCommand(() => StatusContext.RunBlockingTask(UpdateSelectedPointGeoDataElevation));

            UserContent = new UserSiteContent {Fingerprint = Guid.NewGuid()};

            BodyContentFormatContext = new ContentFormatChooserContext();
            BodyContentFormatContext.OnSelectedValueChanged += (sender, s) => UserContent.BodyContentFormat = s;
            UserContent.BodyContentFormat =
                Enum.GetName(typeof(ContentFormatEnum), BodyContentFormatContext.SelectedContentFormat);
            UpdateNotesFormatContext = new ContentFormatChooserContext();
            UpdateNotesFormatContext.OnSelectedValueChanged += (sender, s) => UserContent.UpdateNotesFormat = s;
            UserContent.UpdateNotesFormat =
                Enum.GetName(typeof(ContentFormatEnum), UpdateNotesFormatContext.SelectedContentFormat);
            MainImageFormatContext = new MainImageFormatChooserContext();
            MainImageFormatContext.OnSelectedValueChanged += (sender, s) => UserContent.MainImageFormat = s;
            UserContent.MainImageFormat =
                Enum.GetName(typeof(ContentFormatEnum), MainImageFormatContext.SelectedContentFormat);
        }

        public ContentFormatChooserContext BodyContentFormatContext
        {
            get => _bodyContentFormatContext;
            set
            {
                if (Equals(value, _bodyContentFormatContext)) return;
                _bodyContentFormatContext = value;
                OnPropertyChanged();
            }
        }

        public GeoDataPickerContext GeoDataPickerContext { get; set; }

        public MainImageFormatChooserContext MainImageFormatContext
        {
            get => _mainImageFormatContext;
            set
            {
                if (Equals(value, _mainImageFormatContext)) return;
                _mainImageFormatContext = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand SaveContentCommand { get; set; }
        public RelayCommand SelectGeoDataCommand { get; set; }

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

        public ContentFormatChooserContext UpdateNotesFormatContext
        {
            get => _updateNotesFormatContext;
            set
            {
                if (Equals(value, _updateNotesFormatContext)) return;
                _updateNotesFormatContext = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand UpdateSelectedGeoDataElevation { get; set; }

        public UserSiteContent UserContent
        {
            get => _userContent;
            set
            {
                if (Equals(value, _userContent)) return;
                _userContent = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void LoadExistingData(string slugToLoad)
        {
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task SaveContent()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            UserContent.CleanStrings();

            if (string.IsNullOrWhiteSpace(UserContent.Code))
            {
                StatusContext.ToastWarning("Code can not be blank or only whitespace.");
                return;
            }

            var context = Db.Context();

            var allPreviousVersionsInContent = await context.PointContents
                .Where(x => x.Fingerprint == UserContent.Fingerprint).ToListAsync();

            if (UserContent.Id > 0 && !allPreviousVersionsInContent.Any())
                if ("No" == await StatusContext.ShowMessage("Db Conflict",
                        "The version you started editing is not active in the database (perhaps it was deleted while " +
                        "you were working?) - do you want to continue saving and create a 'new' active entry?",
                        new List<string> {"Yes", "No"}))
                    return;

            var differentVersionInDatabase = allPreviousVersionsInContent.Where(x => x.Id == UserContent.Id).ToList();

            if (differentVersionInDatabase.Any())
                if ("No" == await StatusContext.ShowMessage("Db Conflict",
                        $"There is a version in the database that was updated on {differentVersionInDatabase.First().LastUpdatedOn:g} by " +
                        $"{differentVersionInDatabase.First().LastUpdatedBy} - this is different than the version you started from. Saving " +
                        "will overwrite the updated changes in the database - you may want to look at the saved version and manually merge " +
                        "changes? Continue saving and overwrite changes in the database?",
                        new List<string> {"Yes", "No"}))
                    return;

            foreach (var loopOtherVersions in differentVersionInDatabase)
            {
                var newHistoric = new HistoricPointContent();
                newHistoric.InjectFrom(loopOtherVersions);
                newHistoric.Id = 0;
                context.HistoricPointContents.Add(newHistoric);
            }

            await context.SaveChangesAsync();

            context.PointContents.RemoveRange(differentVersionInDatabase);

            await context.SaveChangesAsync();

            var toAdd = new PointContent();
            toAdd.InjectFrom(UserContent);
            toAdd.Id = 0;
            context.PointContents.Add(toAdd);

            await context.SaveChangesAsync();
        }

        private async Task SelectGeoData()
        {
            var selectedPoints = GeoDataPickerContext.SelectedPoints.ToList();
            var selectedLines = GeoDataPickerContext.SelectedLines.ToList();

            if (selectedPoints.Count + selectedLines.Count > 1)
            {
                StatusContext.ToastError("Sorry - please select only one point or one line...");
                return;
            }

            if (selectedPoints.Count > 0)
            {
                UserContent.LocationDataType = LocationDataTypeConsts.Point;

                var point = selectedPoints.First().Location;
                double elevation;

                if (point.Elevation == null)
                    elevation = await GoogleElevationService.GetElevation(_httpClient,
                        UserSettingsUtilities.ReadSettings().GoogleMapsApiKey, point.Longitude, point.Latitude);
                else
                    elevation = point.Elevation.Value;

                UserContent.LocationData = SpatialHelpers.Wgs84Point(point.Longitude, point.Latitude, elevation);
            }

            if (selectedLines.Count > 0)
            {
                UserContent.LocationDataType = LocationDataTypeConsts.Line;

                var coordinateList = new List<Coordinate>();

                foreach (var location in selectedLines.First().Locations)
                    coordinateList.Add(new Coordinate(location.Longitude, location.Latitude));

                UserContent.LocationData = new LineString(coordinateList.ToArray());
            }
        }

        private async Task UpdateLineElevation(IProgress<string> progress)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var line = (LineString) UserContent.LocationData;

            var totalPoints = line.Coordinates.Length;
            var currentPointNumber = 0;

            var lineStringFactory = DotSpatialAffineCoordinateSequenceFactory.Instance;
            var newLineStringSequence = lineStringFactory.Create(totalPoints, Ordinates.XYZ);

            foreach (var loopCoordinate in line.Coordinates)
            {
                progress.Report(
                    $"Point {currentPointNumber} of {totalPoints} - existing elevation is {loopCoordinate.M}m - " +
                    $"lat {loopCoordinate.Y} long {loopCoordinate.X}");

                var elevation = await GoogleElevationService.GetElevation(_httpClient,
                    UserSettingsUtilities.ReadSettings().GoogleMapsApiKey, loopCoordinate.Y, loopCoordinate.X);

                progress.Report($"Found {elevation}m");

                newLineStringSequence.SetOrdinate(currentPointNumber, Ordinate.X, loopCoordinate.X);
                newLineStringSequence.SetOrdinate(currentPointNumber, Ordinate.Y, loopCoordinate.Y);
                newLineStringSequence.SetOrdinate(currentPointNumber, Ordinate.Z, elevation);

                currentPointNumber++;
            }

            UserContent.LocationData = SpatialHelpers.Wgs84GeometryFactory().CreateLineString(newLineStringSequence);
        }

        private async Task UpdatePointElevation(IProgress<string> progress)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var point = (Point) UserContent.LocationData;

            progress.Report(
                $"Querying for Elevation - existing elevation is {point.Z}m - lat {point.Y} long {point.X}");

            var elevation = await GoogleElevationService.GetElevation(_httpClient,
                UserSettingsUtilities.ReadSettings().GoogleMapsApiKey, point.Y, point.X);

            progress.Report($"Found {elevation}m");

            UserContent.LocationData = SpatialHelpers.Wgs84Point(point.X, point.Y, point.Z);
        }

        private async Task UpdateSelectedPointGeoDataElevation()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (UserContent.LocationData == null)
            {
                StatusContext.ToastError("No data selected?");
                return;
            }

            if (UserContent.LocationDataType == LocationDataTypeConsts.Point)
                await UpdatePointElevation(StatusContext.ProgressTracker());
            if (UserContent.LocationDataType == LocationDataTypeConsts.Line)
                await UpdateLineElevation(StatusContext.ProgressTracker());
        }
    }
}