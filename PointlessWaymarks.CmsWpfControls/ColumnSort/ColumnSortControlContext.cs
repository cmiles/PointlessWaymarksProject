using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms.VisualStyles;
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
            var currentSortCount = Items.Count(x => x.Order > 0);
            if (Items.Count(x => x.Order > 0) == 0 || (currentSortCount == 1 && x.Order > 0))
            {
                ToggleItem(x);
                return;
            }
            
            if (x.Order > 0 && x.SortDirection != x.DefaultSortDirection)
            {
                x.SortDirection = x.DefaultSortDirection;
                x.Order = 0;
                OrderSorts();
                return;
            }

            if (x.Order > 0)
            {
                x.SortDirection = ChangeSortDirection(x.SortDirection);
                return;
            }

            x.SortDirection = x.DefaultSortDirection;
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
                    loopSorts.SortDirection));

            return returnList;
        }

        private void ToggleItem(ColumnSortControlSortItem x)
        {
            if (x.Order == 1)
            {
                x.SortDirection = ChangeSortDirection(x.SortDirection);
                return;
            }

            x.SortDirection = x.DefaultSortDirection;
            x.Order = 1;

            Items.Where(y => y != x)
                .ToList()
                .ForEach(y =>
                {
                    y.Order = 0;
                    y.SortDirection = x.DefaultSortDirection;
                });
        }

        public ListSortDirection ChangeSortDirection(ListSortDirection currentDirection)
        {
            if (currentDirection == ListSortDirection.Ascending) return ListSortDirection.Descending;
            return ListSortDirection.Ascending;
        }
    }
}