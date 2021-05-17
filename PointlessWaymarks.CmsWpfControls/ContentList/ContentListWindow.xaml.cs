using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.ContentList
{
    public partial class ContentListWindow : INotifyPropertyChanged
    {
        private ContentListContext _listContext;

        public ContentListWindow()
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();
            DataContext = this;

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        private readonly Func<int?, IProgress<string>, Task<List<object>>> _loadItemsFunction;
        private readonly Func<int?, Task<bool>> _allItemsLoadedCheck;
        private readonly int? _partialLoadQuantity;
        
        public ContentListWindow(
            Func<int?, IProgress<string>, Task<List<object>>> loadItemsFunction, Func<int?, Task<bool>> allItemsLoadedCheck, int? partialLoadQuantity)
        {
            InitializeComponent();

            _partialLoadQuantity = partialLoadQuantity;
            _loadItemsFunction = loadItemsFunction;
            _allItemsLoadedCheck = allItemsLoadedCheck;
            
            StatusContext = new StatusControlContext();
            DataContext = this;

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

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var listContext = new ContentListContext(StatusContext,_loadItemsFunction, _allItemsLoadedCheck,  _partialLoadQuantity);

            await listContext.LoadData();

            ListContext = listContext;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}