using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.NoteList
{
    public class NoteListContext : INotifyPropertyChanged
    {
        private ObservableRangeCollection<NoteListListItem> _items;
        private List<NoteListListItem> _selectedItems;
        private StatusControlContext _statusContext;

        public NoteListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public ObservableRangeCollection<NoteListListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }


        public List<NoteListListItem> SelectedItems
        {
            get => _selectedItems;
            set
            {
                if (Equals(value, _selectedItems)) return;
                _selectedItems = value;
                OnPropertyChanged();
            }
        }

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

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            var dbItems = db.NoteContents.ToList();
            var listItems = new List<NoteListListItem>();

            foreach (var loopItems in dbItems)
            {
                var newItem = new NoteListListItem {DbEntry = loopItems};

                listItems.Add(newItem);
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            Items = new ObservableRangeCollection<NoteListListItem>(listItems);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}