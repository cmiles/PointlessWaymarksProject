using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Amazon.S3.Model;
using JetBrains.Annotations;

namespace PointlessWaymarks.CmsWpfControls.ColumnSort
{
    /// <summary>
    /// Interaction logic for ColumnSortControl.xaml
    /// </summary>
    public partial class ColumnSortControl : UserControl
    {
        public ColumnSortControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ColumnSortItemsProperty = DependencyProperty.Register(
            "ColumnSortItems", typeof(ObservableCollection<SortItem>), typeof(ColumnSortControl), new PropertyMetadata(default(ObservableCollection<SortItem>)));

        public ObservableCollection<SortItem> ColumnSortItems
        {
            get => (ObservableCollection<SortItem>) GetValue(ColumnSortItemsProperty);
            set => SetValue(ColumnSortItemsProperty, value);
        }
    }

    public class SortItem : INotifyPropertyChanged
    {
        private string _columnName;
        private bool _ascending;
        private int _order;

        public int Order
        {
            get => _order;
            set
            {
                if (value == _order) return;
                _order = value;
                OnPropertyChanged();
            }
        }

        public bool Ascending
        {
            get => _ascending;
            set
            {
                if (value == _ascending) return;
                _ascending = value;
                OnPropertyChanged();
            }
        }

        public string ColumnName
        {
            get => _columnName;
            set
            {
                if (value == _columnName) return;
                _columnName = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
