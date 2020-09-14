#nullable enable
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.WebViewTwoScratch
{
    /// <summary>
    ///     Interaction logic for WebViewTwoScratchWindow.xaml
    /// </summary>
    public partial class WebViewTwoScratchWindow : INotifyPropertyChanged
    {
        private StatusControlContext _statusContext;
        private WebViewThreeContext _webFour;
        private WebViewTwoContext _webOne;
        private WebViewTwoContext _webThree;
        private WebViewTwoContext _webTwo;

        public WebViewTwoScratchWindow()
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();

            DataContext = this;

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
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

        public WebViewThreeContext WebFour
        {
            get => _webFour;
            set
            {
                if (Equals(value, _webFour)) return;
                _webFour = value;
                OnPropertyChanged();
            }
        }

        public WebViewTwoContext WebOne
        {
            get => _webOne;
            set
            {
                if (Equals(value, _webOne)) return;
                _webOne = value;
                OnPropertyChanged();
            }
        }

        public WebViewTwoContext WebThree
        {
            get => _webThree;
            set
            {
                if (Equals(value, _webThree)) return;
                _webThree = value;
                OnPropertyChanged();
            }
        }

        public WebViewTwoContext WebTwo
        {
            get => _webTwo;
            set
            {
                if (Equals(value, _webTwo)) return;
                _webTwo = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async Task LoadData()
        {
            WebOne = WebViewTwoContext.CreateInstance(StatusContext);
            WebTwo = WebViewTwoContext.CreateInstance(StatusContext);
            WebThree = WebViewTwoContext.CreateInstance(StatusContext);
            WebFour = WebViewThreeContext.CreateInstance(StatusContext);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}