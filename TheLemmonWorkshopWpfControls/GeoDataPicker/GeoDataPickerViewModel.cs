using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using MapControl;
using Microsoft.Win32;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TheLemmonWorkshopData;
using TheLemmonWorkshopData.Elevation;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.Utility;
using TheLemmonWorkshopWpfControls.XamlMapConstructs;
using Location = MapControl.Location;

namespace TheLemmonWorkshopWpfControls.GeoDataPicker
{
    public class GeoDataPickerViewModel : INotifyPropertyChanged
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private string _fileName;
        private List<MapDisplayPolyline> _selectedLines = new List<MapDisplayPolyline>();
        private MapDisplayPoint _selectedPoint;
        private List<MapDisplayPoint> _selectedPoints = new List<MapDisplayPoint>();
        private MapDisplayPolyline _selectedPolyline;
        private StandardMapViewModel _standardMapContext;
        private ControlStatusViewModel _statusContext;

        public GeoDataPickerViewModel(ControlStatusViewModel statusContext)
        {
            StatusContext = statusContext;

            StartLoadCommand = new RelayCommand(() =>
                StatusContext.RunBlockingTask(() => LoadFile(StatusContext.ProgressTracker())));
            ClearItemsCommand = new RelayCommand(() =>
                StatusContext.RunBlockingTask(() => ClearList(StatusContext.ProgressTracker())));
            SelectItemCommand = new RelayCommand(async () => await ReturnSelected());

            PointsListSelectionChangedCommand = new RelayCommand<IList>(PointsListSelectionChanged);
            PolylinesListSelectionChangedCommand = new RelayCommand<IList>(PolylinesListSelectionChanged);

            StandardMapContext = new StandardMapViewModel(statusContext);
        }

        public event EventHandler<SelectedGeoData> GeoDataSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        public RelayCommand ClearItemsCommand { get; set; }
        public RelayCommand ClearSelectionCommand { get; set; }

        public string FileName
        {
            get => _fileName;
            set
            {
                if (value == _fileName) return;
                _fileName = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand<IList> PointsListSelectionChangedCommand { get; set; }
        public RelayCommand<IList> PolylinesListSelectionChangedCommand { get; set; }

        public List<MapDisplayPolyline> SelectedLines
        {
            get => _selectedLines;
            set
            {
                if (Equals(value, _selectedLines)) return;
                _selectedLines = value;
                OnPropertyChanged();

                StandardMapContext.OnMapPolylineSelectionRequest(this, SelectedLines);
            }
        }

        public MapDisplayPoint SelectedPoint
        {
            get => _selectedPoint;
            set
            {
                if (Equals(value, _selectedPoint)) return;
                _selectedPoint = value;
                OnPropertyChanged();
            }
        }

        public List<MapDisplayPoint> SelectedPoints
        {
            get => _selectedPoints;
            set
            {
                if (Equals(value, _selectedPoints)) return;
                _selectedPoints = value;
                OnPropertyChanged();

                StandardMapContext.OnMapPointSelectionRequest(this, SelectedPoints);
            }
        }

        public MapDisplayPolyline SelectedPolyline
        {
            get => _selectedPolyline;
            set
            {
                if (Equals(value, _selectedPolyline)) return;
                _selectedPolyline = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand SelectItemCommand { get; set; }

        public StandardMapViewModel StandardMapContext
        {
            get => _standardMapContext;
            set
            {
                if (Equals(value, _standardMapContext)) return;
                _standardMapContext = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand StartLoadCommand { get; set; }

        public ControlStatusViewModel StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public async Task ClearList(IProgress<string> progress)
        {
            progress.Report("Clearing List");

            await ThreadSwitcher.ResumeForegroundAsync();

            StandardMapContext.Points.Clear();
            StandardMapContext.Polylines.Clear();

            progress.Report("Cleared List Items");
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task LoadFile(IProgress<string> progress)
        {
            progress.Report("Get File...");

            await ThreadSwitcher.ResumeForegroundAsync();

            var fileDialog = new OpenFileDialog { DefaultExt = ".gpx", Filter = "GPX Files (*.gpx)|*.gpx|All Files|*.*" };

            var result = fileDialog.ShowDialog();

            progress.Report("Get File Ended...");

            if (result == true)
            {
                // Open document
                FileName = fileDialog.FileName;
                progress.Report($"Picked {FileName}...");
            }
            else
            {
                StatusContext.ToastWarning("Loading cancelled");
                return;
            }

            progress.Report("Checking File");

            if (string.IsNullOrWhiteSpace(FileName))
            {
                StatusContext.ToastError("Please pick a file");
                return;
            }

            var gpxFileInfo = new FileInfo(FileName);

            if (!gpxFileInfo.Exists)
            {
                StatusContext.ToastError($"File {FileName} doesn't exist?");
                return;
            }

            progress.Report("Parsing Waypoints...");

            await ThreadSwitcher.ResumeBackgroundAsync();

            var gpxParse = GpxFile.Parse(File.ReadAllText(gpxFileInfo.FullName), new GpxReaderSettings());
            var waypoints = gpxParse.Waypoints;

            StatusContext.ToastSuccess($"Found {waypoints.Count} GPX Waypoints");

            await ThreadSwitcher.ResumeForegroundAsync();

            foreach (var loopWaypoint in waypoints)
            {
                var idGuid = Guid.NewGuid();

                StandardMapContext.Points.Add(new MapDisplayPoint
                {
                    Id = idGuid,
                    Name = loopWaypoint.Name,
                    Location =
                        new MapLocationZ(loopWaypoint.Latitude, loopWaypoint.Longitude,
                            loopWaypoint.ElevationInMeters),
                    ExtendedDescription = "Waypoint - " + loopWaypoint.Comment +
                                          (loopWaypoint.ElevationInMeters == null
                                              ? string.Empty
                                              : $" Elevation {loopWaypoint.ElevationInMeters.MetersToFeet()}'")
                });
            }

            var tracks = gpxParse.Tracks;

            StatusContext.ToastSuccess($"Found {tracks.Count} GPX Tracks");

            foreach (var loopTrack in tracks)
            {
                var idGuid = Guid.NewGuid();

                var pointList = new List<MapLocationZ>();

                foreach (var loopSegments in loopTrack.Segments)
                    foreach (var loopPoints in loopSegments.Waypoints)
                        pointList.Add(new MapLocationZ(loopPoints.Latitude, loopPoints.Longitude,
                            loopPoints.ElevationInMeters));

                StandardMapContext.Polylines.Add(new MapDisplayPolyline
                {
                    Id = idGuid,
                    Locations = new LocationCollection(pointList),
                    Name = loopTrack.Name,
                    ExtendedDescription = "Track - " + loopTrack.Name
                });
            }

            if (StandardMapContext.Points.Any())
            {
                StandardMapContext.MapCenter = new Location(StandardMapContext.Points.First().Location.Latitude,
                    StandardMapContext.Points.First().Location.Longitude);
            }
            else
            {
                if (StandardMapContext.Polylines.Any())
                    StandardMapContext.MapCenter =
                        new Location(StandardMapContext.Polylines.First().Locations.First().Latitude,
                            StandardMapContext.Polylines.First().Locations.First().Longitude);
            }
        }

        private void PointsListSelectionChanged(IList listOfPoints)
        {
            if (listOfPoints == null || listOfPoints.Count == 0)
            {
                SelectedPoints = new List<MapDisplayPoint>();
                return;
            }

            var newSelection = new List<MapDisplayPoint>();

            foreach (var loopPoints in listOfPoints)
            {
                if (loopPoints is MapDisplayPoint toAdd) newSelection.Add(toAdd);
            }

            SelectedPoints = newSelection;
        }

        private void PolylinesListSelectionChanged(IList listOfLines)
        {
            if (listOfLines == null || listOfLines.Count == 0)
            {
                SelectedLines = new List<MapDisplayPolyline>();
                return;
            }

            var newSelection = new List<MapDisplayPolyline>();

            foreach (var loopLines in listOfLines)
            {
                if (loopLines is MapDisplayPolyline toAdd) newSelection.Add(toAdd);
            }

            SelectedLines = newSelection;
        }

        private async Task ReturnSelected()
        {
            if (SelectedPoints.Count != 1)
            {
                StatusContext.ToastError("Please select 1 point...");
                return;
            }

            var selectedPoint = SelectedPoints[0];

            var possiblePoint = StandardMapContext.Points.SingleOrDefault(x => x.Id == selectedPoint.Id);
            if (possiblePoint != null)
            {
                var newPoint = new Point(possiblePoint.Location.Longitude, possiblePoint.Location.Latitude);

                if (possiblePoint.Location.Elevation == null)
                {
                    try
                    {
                        var elevation = await GoogleElevationService.GetElevation(HttpClient,
                            UserSettingsUtilities.ReadSettings().GoogleMapsApiKey, possiblePoint.Location.Latitude,
                            possiblePoint.Location.Longitude);
                        newPoint.M = elevation;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                GeoDataSelected?.Invoke(this,
                    new SelectedGeoData { GeoType = LocationDataTypeConsts.Point, GeoData = newPoint });

                return;
            }

            var possibleLine = StandardMapContext.Polylines.SingleOrDefault(x => x.Id == selectedPoint.Id);

            if (possibleLine != null)
            {
                var ntsLineString = new LineString(possibleLine.Locations
                    .Select(x => new Coordinate(x.Longitude, x.Latitude)).ToArray());
                GeoDataSelected?.Invoke(this,
                    new SelectedGeoData { GeoType = LocationDataTypeConsts.Line, GeoData = ntsLineString });
            }
        }
    }
}