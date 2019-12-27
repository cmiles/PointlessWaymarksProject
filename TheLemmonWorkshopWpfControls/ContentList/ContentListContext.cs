using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.ContentList
{
    public class ContentListContext : INotifyPropertyChanged
    {
        private StatusControlContext _statusContext;

        public ContentListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext;

            StatusContext.RunBlockingTask(LoadAllContent);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<ContentListItem> Items { get; set; }

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

        public async Task LoadAllContent()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = Db.Context();

            var rawList = await db.PointContents.ToListAsync();

            var processedList = rawList.Select(x => new ContentListItem {Title = x.Title, Summary = x.Summary});

            Items = new ObservableCollection<ContentListItem>(processedList);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}