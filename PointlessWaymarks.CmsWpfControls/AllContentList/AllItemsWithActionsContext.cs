using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.WordPressXmlImport;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.AllContentList
{
    public class AllItemsWithActionsContext : INotifyPropertyChanged
    {
        private ContentListContext _listContext;
        private Command _wordPressImportWindowCommand;

        public AllItemsWithActionsContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
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

        public StatusControlContext StatusContext { get; set; }

        public Command WordPressImportWindowCommand
        {
            get => _wordPressImportWindowCommand;
            set
            {
                if (Equals(value, _wordPressImportWindowCommand)) return;
                _wordPressImportWindowCommand = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            ListContext ??= new ContentListContext(StatusContext, new ContentListLoaderAllItems(100));

            WordPressImportWindowCommand = StatusContext.RunNonBlockingTaskCommand(WordPressImportWindow);

            ListContext.ContextMenuItems = new List<ContextMenuItemData>
            {
                new() {ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand},
                new()
                {
                    ItemName = "Code to Clipboard", ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
                },
                new()
                    {ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand},
                new() {ItemName = "Open URL", ItemCommand = ListContext.OpenUrlSelectedCommand},
                new() {ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand},
                new()
                    {ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand}
            };

            await ListContext.LoadData();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task WordPressImportWindow()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            new WordPressXmlImportWindow().PositionWindowAndShow();
        }
    }
}