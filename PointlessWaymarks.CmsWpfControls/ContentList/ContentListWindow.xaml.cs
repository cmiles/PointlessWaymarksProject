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
        private readonly IContentListLoader _loader;
        private ContentListContext _listContext;

        public ContentListWindow()
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();
            DataContext = this;

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public ContentListWindow(IContentListLoader loader)
        {
            InitializeComponent();

            _loader = loader;

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

            var listContext = new ContentListContext(StatusContext, _loader);

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