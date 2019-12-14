using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TheLemmonWorkshopWpfControls.XamlMapConstructs;

namespace TheLemmonWorkshopWpfControls.GeoDataPicker
{
    /// <summary>
    /// Interaction logic for GeoDataPickerControl.xaml
    /// </summary>
    public partial class GeoDataPickerControl
    {
        public GeoDataPickerControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(DataContext is GeoDataPickerContext model)) return;

            model.StandardMapContext.ListPointSelectionRequest += StandardMapContextOnListPointSelectionRequest;
            model.StandardMapContext.ListPolylineSelectionRequest += StandardMapContextOnListPolyLineSelectionRequest;
        }

        private void StandardMapContextOnListPointSelectionRequest(object sender, List<MapDisplayPoint> e)
        {
            if (e == null || e.Count == 0)
            {
                PointsList.SelectedItems.Clear();
                return;
            }

            if (PointsList.SelectedItems.Count == 0)
            {
                e.ForEach(x => PointsList.SelectedItems.Add(x));
                return;
            }

            var alreadySelected = new List<MapDisplayPoint>();
            var toRemove = new List<MapDisplayPoint>();

            foreach (var loopSelected in PointsList.SelectedItems)
            {
                var item = (MapDisplayPoint)loopSelected;
                if (e.Contains(item)) alreadySelected.Add(item);
                if (!e.Contains(item)) toRemove.Add(item);
            }

            var toAdd = e.Except(toRemove.Concat(alreadySelected)).ToList();

            toRemove.ForEach(x => PointsList.SelectedItems.Remove(x));
            toAdd.ForEach(x => PointsList.SelectedItems.Add(x));
        }

        private void StandardMapContextOnListPolyLineSelectionRequest(object sender, List<MapDisplayPolyline> e)
        {
            if (e == null || e.Count == 0)
            {
                LinesList.SelectedItems.Clear();
                return;
            }

            if (LinesList.SelectedItems.Count == 0)
            {
                e.ForEach(x => LinesList.SelectedItems.Add(x));
                return;
            }

            var alreadySelected = new List<MapDisplayPolyline>();
            var toRemove = new List<MapDisplayPolyline>();

            foreach (var loopSelected in LinesList.SelectedItems)
            {
                var item = (MapDisplayPolyline)loopSelected;
                if (e.Contains(item)) alreadySelected.Add(item);
                if (!e.Contains(item)) toRemove.Add(item);
            }

            var toAdd = e.Except(toRemove.Concat(alreadySelected)).ToList();

            toRemove.ForEach(x => LinesList.SelectedItems.Remove(x));
            toAdd.ForEach(x => LinesList.SelectedItems.Add(x));
        }
    }
}