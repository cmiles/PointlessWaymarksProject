using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.AllContentList
{
    public class AllItemsWithActionsContext : INotifyPropertyChanged
    {
        private Command _importFromExcelFileCommand;
        private ContentListContext _listContext;
        private Command _selectedToExcelCommand;

        public AllItemsWithActionsContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public Command ImportFromExcelFileCommand
        {
            get => _importFromExcelFileCommand;
            set
            {
                if (Equals(value, _importFromExcelFileCommand)) return;
                _importFromExcelFileCommand = value;
                OnPropertyChanged();
            }
        }

        public ContentListContext ListContext
        {
            get => _listContext;
            set
            {
                if (Equals(value, _listContext)) return;
                _listContext = value;
                OnPropertyChanged();
            }
        }

        public Command SelectedToExcelCommand
        {
            get => _selectedToExcelCommand;
            set
            {
                if (Equals(value, _selectedToExcelCommand)) return;
                _selectedToExcelCommand = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext = new ContentListContext(StatusContext, new ContentListLoaderAllItems(100));

            SelectedToExcelCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
                await ExcelHelpers.SelectedToExcel(ListContext.ListSelection.SelectedItems?.Cast<dynamic>().ToList(),
                    StatusContext));
            ImportFromExcelFileCommand =
                StatusContext.RunBlockingTaskCommand(async () => await ExcelHelpers.ImportFromExcelFile(StatusContext));

            await ListContext.LoadData();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}