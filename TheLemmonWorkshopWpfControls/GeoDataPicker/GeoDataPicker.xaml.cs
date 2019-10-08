using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TheLemmonWorkshopWpfControls.XamlMapConstructs;

namespace TheLemmonWorkshopWpfControls.GeoDataPicker
{
    /// <summary>
    /// Interaction logic for GeoDataPicker.xaml
    /// </summary>
    public partial class GeoDataPicker
    {
        public GeoDataPicker()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

            var model = DataContext as GeoDataPickerViewModel;

            if (model == null) return;

            model.StandardMapContext.ListPointSelectionRequest += StandardMapContextOnListPointSelectionRequest;
            
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

            if (e.Count == PointsList.SelectedItems.Count) return;

            var alreadySelected = new List<MapDisplayPoint>();
            var toRemove = new List<MapDisplayPoint>();

            foreach (var loopSelected in PointsList.SelectedItems)
            {
                var item = (MapDisplayPoint) loopSelected;
                if (e.Contains(item)) alreadySelected.Add(item);
                if(!e.Contains(item)) toRemove.Add(item);
            }

            var toAdd = e.Except(toRemove.Concat(alreadySelected)).ToList();

            toRemove.ForEach(x => PointsList.SelectedItems.Remove(x));
            toAdd.ForEach(x => PointsList.SelectedItems.Add(x));
        }
    }
}