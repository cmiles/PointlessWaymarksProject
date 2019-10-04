using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Omu.ValueInjecter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TheLemmonWorkshopData;
using TheLemmonWorkshopData.Models;
using TheLemmonWorkshopWpfControls.ContentFormat;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.GeoDataPicker;
using TheLemmonWorkshopWpfControls.MainImageFormat;
using TheLemmonWorkshopWpfControls.Models;
using TheLemmonWorkshopWpfControls.Utility;
using TheLemmonWorkshopWpfControls.XamlMapConstructs;

namespace TheLemmonWorkshopWpfControls.ItemContentEditor
{
    public class ItemContentEditorViewModel : INotifyPropertyChanged
    {
        private ContentFormatChooserViewModel _bodyContentFormatContext;
        private MainImageFormatChooserViewModel _mainImageFormatContext;
        private StandardMapViewModel _standardMapContext;
        private ControlStatusViewModel _statusContext;
        private ContentFormatChooserViewModel _updateNotesFormatContext;
        private UserSiteContent _userContent;

        public ItemContentEditorViewModel()
        {
            StatusContext = new ControlStatusViewModel();
            StandardMapContext = new StandardMapViewModel(StatusContext);

            ShowGeoPickerCommand = new RelayCommand(() => StatusContext.RunBlockingTask(ShowGeoPicker));
            SaveContentCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SaveContent));

            UserContent = new UserSiteContent { Fingerprint = Guid.NewGuid() };

            BodyContentFormatContext = new ContentFormatChooserViewModel();
            BodyContentFormatContext.OnSelectedValueChanged += (sender, s) => UserContent.BodyContentFormat = s;
            UserContent.BodyContentFormat =
                Enum.GetName(typeof(ContentFormatEnum), BodyContentFormatContext.SelectedContentFormat);
            UpdateNotesFormatContext = new ContentFormatChooserViewModel();
            UpdateNotesFormatContext.OnSelectedValueChanged += (sender, s) => UserContent.UpdateNotesFormat = s;
            UserContent.UpdateNotesFormat =
                Enum.GetName(typeof(ContentFormatEnum), UpdateNotesFormatContext.SelectedContentFormat);
            MainImageFormatContext = new MainImageFormatChooserViewModel();
            MainImageFormatContext.OnSelectedValueChanged += (sender, s) => UserContent.MainImageFormat = s;
            UserContent.MainImageFormat =
                Enum.GetName(typeof(ContentFormatEnum), MainImageFormatContext.SelectedContentFormat);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ContentFormatChooserViewModel BodyContentFormatContext
        {
            get => _bodyContentFormatContext;
            set
            {
                if (Equals(value, _bodyContentFormatContext)) return;
                _bodyContentFormatContext = value;
                OnPropertyChanged();
            }
        }

        public MainImageFormatChooserViewModel MainImageFormatContext
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

        public RelayCommand ShowGeoPickerCommand { get; set; }

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

        public ContentFormatChooserViewModel UpdateNotesFormatContext
        {
            get => _updateNotesFormatContext;
            set
            {
                if (Equals(value, _updateNotesFormatContext)) return;
                _updateNotesFormatContext = value;
                OnPropertyChanged();
            }
        }

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

        public void LoadExistingData(string slugToLoad)
        {
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void GeoDataPickerContextOnGeoDataSelected(object sender, SelectedGeoData e)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (e == null) return;
            UserContent.LocationDataType = e.GeoType;
            UserContent.LocationData = e.GeoData;

            await ThreadSwitcher.ResumeForegroundAsync();

            StandardMapContext.Points.Clear();
            StandardMapContext.Polylines.Clear();

            if (UserContent.LocationDataType == LocationDataTypeConsts.Point)
            {
                StandardMapContext.Points.Add(new MapDisplayPoint
                {
                    Location = new MapLocationM(((Point)UserContent.LocationData).Coordinate.Y,
                        ((Point)UserContent.LocationData).Coordinate.X,
                        ((Point)UserContent.LocationData).Coordinate.M)
                });
                StandardMapContext.MapCenter = StandardMapContext.Points.First().Location;
            }
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

            var allPreviousVersionsInContent = await context.SiteContents
                .Where(x => x.Fingerprint == UserContent.Fingerprint).ToListAsync();

            if (UserContent.Id > 0 && !allPreviousVersionsInContent.Any())
                if ("No" == await StatusContext.ShowMessage("Db Conflict",
                        "The version you started editing is not active in the database (perhaps it was deleted while " +
                        "you were working?) - do you want to continue saving and create a 'new' active entry?",
                        new List<string> { "Yes", "No" }))
                    return;

            var differentVersionInDatabase = allPreviousVersionsInContent.Where(x => x.Id == UserContent.Id).ToList();

            if (differentVersionInDatabase.Any())
                if ("No" == await StatusContext.ShowMessage("Db Conflict",
                        $"There is a version in the database that was updated on {differentVersionInDatabase.First().LastUpdatedOn:g} by " +
                        $"{differentVersionInDatabase.First().LastUpdatedBy} - this is different than the version you started from. Saving " +
                        "will overwrite the updated changes in the database - you may want to look at the saved version and manually merge " +
                        "changes? Continue saving and overwrite changes in the database?",
                        new List<string> { "Yes", "No" }))
                    return;

            foreach (var loopOtherVersions in differentVersionInDatabase)
            {
                var newHistoric = new HistoricSiteContent();
                newHistoric.InjectFrom(loopOtherVersions);
                newHistoric.Id = 0;
                context.HistoricSiteContents.Add(newHistoric);
            }

            await context.SaveChangesAsync();

            context.SiteContents.RemoveRange(differentVersionInDatabase);

            await context.SaveChangesAsync();

            var toAdd = new SiteContent();
            toAdd.InjectFrom(UserContent);
            toAdd.Id = 0;
            context.SiteContents.Add(toAdd);

            await context.SaveChangesAsync();
        }

        private async Task ShowGeoPicker()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            var newWindow = new GeoDataPickerWindow();
            newWindow.GeoDataPickerContext.GeoDataSelected += GeoDataPickerContextOnGeoDataSelected;

            newWindow.Show();
        }
    }
}