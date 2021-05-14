using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.WpfCommon.Commands;

namespace PointlessWaymarks.CmsWpfControls.ColumnSort
{
    public class ColumnSortControlContext : INotifyPropertyChanged
    {
        private List<ColumnSortControlSortItem> _items;

        public ColumnSortControlContext()
        {
            ColumnSortToggleCommand = new Command<ColumnSortControlSortItem>(x =>
            {
                ToggleItem(x);
                SortUpdated?.Invoke(this, SortDescriptions());
            });

            ColumnSortAddCommand = new Command<ColumnSortControlSortItem>(x =>
            {
                AddItem(x);
                SortUpdated?.Invoke(this, SortDescriptions());
            });
        }

        public Command<ColumnSortControlSortItem> ColumnSortAddCommand { get; set; }

        public Command<ColumnSortControlSortItem> ColumnSortToggleCommand { get; set; }

        public List<ColumnSortControlSortItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public EventHandler<List<SortDescription>> SortUpdated { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void AddItem(ColumnSortControlSortItem x)
        {
            if (x.Order > 0 && x.Ascending)
            {
                x.Ascending = false;
                x.Order = 0;
                OrderSorts();
                return;
            }

            if (x.Order > 0 && !x.Ascending)
            {
                x.Ascending = true;
                return;
            }

            x.Order = Items.Max(y => y.Order) + 1;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OrderSorts()
        {
            var newOrder = 1;
            Items.Where(x => x.Order > 0).OrderBy(x => x.Order).ToList().ForEach(x => x.Order = newOrder++);
        }

        public List<SortDescription> SortDescriptions()
        {
            var returnList = new List<SortDescription>();

            foreach (var loopSorts in Items.Where(x => x.Order > 0).OrderBy(x => x.Order).ToList())
                returnList.Add(new SortDescription(loopSorts.ColumnName,
                    loopSorts.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending));

            return returnList;
        }

        private void ToggleItem(ColumnSortControlSortItem x)
        {
            if (x.Order == 1 && !x.Ascending)
            {
                x.Ascending = true;
                return;
            }

            if (x.Order == 1)
            {
                x.Order = 0;
                x.Ascending = false;
                return;
            }

            x.Order = 1;

            Items.Where(y => y != x)
                .ToList()
                .ForEach(y =>
                {
                    y.Order = 0;
                    y.Ascending = false;
                });
        }
    }
}