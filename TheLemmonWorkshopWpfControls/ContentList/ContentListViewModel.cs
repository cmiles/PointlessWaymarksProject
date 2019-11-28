using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.ContentList
{
    public class ContentListViewModel : INotifyPropertyChanged
    {
        private ControlStatusViewModel _statusContext;
        public event PropertyChangedEventHandler PropertyChanged;

        public ContentListViewModel(ControlStatusViewModel statusContext)
        {
            StatusContext = statusContext;

            StatusContext.RunBlockingTask(LoadAllContent);
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

        public async Task LoadAllContent()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = Db.Context();

            var rawList = await db.SiteContents.ToListAsync();

            var processedList = rawList.Select(x => new ContentListItem
            {
                Title = x.Title,
                Summary = x.Summary
            });

            Items = new ObservableCollection<ContentListItem>(processedList);
        }

        public ObservableCollection<ContentListItem> Items { get; set; }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
