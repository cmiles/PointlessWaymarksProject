﻿using JetBrains.Annotations;
using MapControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.Utility;
using TheLemmonWorkshopWpfControls.XamlMapConstructs;

namespace TheLemmonWorkshopWpfControls.GeoDataPicker
{
    /// <summary>
    /// Interaction logic for GeoDataPickerWindow.xaml
    /// </summary>
    public partial class GeoDataPickerWindow : INotifyPropertyChanged
    {
        private GeoDataPickerViewModel _geoDataPickerContext;

        public GeoDataPickerWindow()
        {
            InitializeComponent();

            DataContext = this;

            GeoDataPickerContext = new GeoDataPickerViewModel(new ControlStatusViewModel());
            GeoDataPickerContext.GeoDataSelected += (sender, data) => Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public GeoDataPickerViewModel GeoDataPickerContext
        {
            get => _geoDataPickerContext;
            set
            {
                if (Equals(value, _geoDataPickerContext)) return;
                _geoDataPickerContext = value;
                OnPropertyChanged();
            }
        }

        public async Task AddLine(List<(double latitude, double longitude)> line, string name, string description, bool protectFromUserClearing)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var id = Guid.NewGuid();

            await ThreadSwitcher.ResumeForegroundAsync();

            GeoDataPickerContext.StandardMapContext.Polylines.Add(new MapDisplayPolyline()
            {
                Id = id,
                Locations = new LocationCollection(line.Select(x => new Location(x.latitude, x.longitude)))
            });
        }

        public async Task AddPoint(double latitude, double longitude, string name, string description, bool protectFromUserClearing)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var id = Guid.NewGuid();

            await ThreadSwitcher.ResumeForegroundAsync();

            GeoDataPickerContext.StandardMapContext.Points.Add(new MapDisplayPoint
            {
                Id = id,
                Location = new MapLocationZ(latitude, longitude, null),
                Name = name
            });
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}