using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using MvvmHelpers;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Pictures;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.FileList
{
    public class FileListContext : INotifyPropertyChanged
    {
        private RelayCommand _filterListCommand;
        private ObservableRangeCollection<FileListListItem> _items;
        private string _lastSortColumn;
        private List<FileListListItem> _selectedItems;
        private bool _sortDescending;
        private RelayCommand<string> _sortListCommand;
        private StatusControlContext _statusContext;
        private RelayCommand _toggleListSortDirectionCommand;
        private string _userFilterText;

        public FileListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public RelayCommand FilterListCommand
        {
            get => _filterListCommand;
            set
            {
                if (Equals(value, _filterListCommand)) return;
                _filterListCommand = value;
                OnPropertyChanged();
            }
        }

        public ObservableRangeCollection<FileListListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public List<FileListListItem> SelectedItems
        {
            get => _selectedItems;
            set
            {
                if (Equals(value, _selectedItems)) return;
                _selectedItems = value;
                OnPropertyChanged();
            }
        }

        public bool SortDescending
        {
            get => _sortDescending;
            set
            {
                if (value == _sortDescending) return;
                _sortDescending = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand<string> SortListCommand
        {
            get => _sortListCommand;
            set
            {
                if (Equals(value, _sortListCommand)) return;
                _sortListCommand = value;
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

        public RelayCommand ToggleListSortDirectionCommand
        {
            get => _toggleListSortDirectionCommand;
            set
            {
                if (Equals(value, _toggleListSortDirectionCommand)) return;
                _toggleListSortDirectionCommand = value;
                OnPropertyChanged();
            }
        }

        public string UserFilterText
        {
            get => _userFilterText;
            set
            {
                if (value == _userFilterText) return;
                _userFilterText = value;
                OnPropertyChanged();
            }
        }

        private async Task FilterList()
        {
            if (Items == null || !Items.Any()) return;

            await ThreadSwitcher.ResumeForegroundAsync();

            ((CollectionView) CollectionViewSource.GetDefaultView(Items)).Filter = o =>
            {
                if (string.IsNullOrWhiteSpace(UserFilterText)) return true;

                var loweredString = UserFilterText.ToLower();

                if (!(o is FileListListItem pi)) return false;
                if ((pi.DbEntry.Title ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Tags ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Summary ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.CreatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.LastUpdatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.OriginalFileName ?? string.Empty).ToLower().Contains(loweredString)) return true;
                return false;
            };
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            FilterListCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(FilterList));
            SortListCommand = new RelayCommand<string>(x => StatusContext.RunNonBlockingTask(() => SortList(x)));
            ToggleListSortDirectionCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(async () =>
            {
                SortDescending = !SortDescending;
                await SortList(_lastSortColumn);
            }));

            StatusContext.Progress("Connecting to DB");

            var db = await Db.Context();

            StatusContext.Progress("Getting Post Db Entries");
            var dbItems = db.FileContents.ToList();
            var listItems = new List<FileListListItem>();

            var totalCount = dbItems.Count;
            var currentLoop = 1;

            foreach (var loopItems in dbItems)
            {
                if (totalCount == 1 || totalCount % 10 == 0)
                    StatusContext.Progress($"Processing Post Item {currentLoop} of {totalCount}");

                var newFileItem = new FileListListItem {DbEntry = loopItems};

                if (loopItems.MainPicture != null)
                    newFileItem.SmallImageUrl = PictureAssetProcessing
                        .ProcessPictureDirectory(loopItems.MainPicture.Value).SmallPicture?.File.FullName;

                listItems.Add(newFileItem);

                currentLoop++;
            }

            StatusContext.Progress("Displaying Files");

            await ThreadSwitcher.ResumeForegroundAsync();

            Items = new ObservableRangeCollection<FileListListItem>(listItems);

            SortDescending = true;
            await SortList("CreatedOn");
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private async Task SortList(string sortColumn)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            _lastSortColumn = sortColumn;

            var collectionView = ((CollectionView) CollectionViewSource.GetDefaultView(Items));
            collectionView.SortDescriptions.Clear();

            if (string.IsNullOrWhiteSpace(sortColumn)) return;
            collectionView.SortDescriptions.Add(new SortDescription($"DbEntry.{sortColumn}",
                SortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}