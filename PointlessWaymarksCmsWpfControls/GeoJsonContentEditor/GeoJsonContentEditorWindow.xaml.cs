using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;
using PointlessWaymarksCmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarksCmsWpfControls.Utility.ThreadSwitcher;

namespace PointlessWaymarksCmsWpfControls.GeoJsonContentEditor
{
    /// <summary>
    ///     Interaction logic for GeoJsonContentEditorWindow.xaml
    /// </summary>
    public partial class GeoJsonContentEditorWindow : INotifyPropertyChanged
    {
        private GeoJsonContentEditorContext _geoJsonContent;
        private StatusControlContext _statusContext;

        public GeoJsonContentEditorWindow(GeoJsonContent toLoad)
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(async () =>
            {
                GeoJsonContent = await GeoJsonContentEditorContext.CreateInstance(StatusContext, toLoad);

                GeoJsonContent.RequestLinkContentEditorWindowClose += (sender, args) => { Dispatcher?.Invoke(Close); };
                AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, GeoJsonContent);

                await ThreadSwitcher.ResumeForegroundAsync();
                DataContext = this;
            });
        }

        public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }

        public GeoJsonContentEditorContext GeoJsonContent
        {
            get => _geoJsonContent;
            set
            {
                if (Equals(value, _geoJsonContent)) return;
                _geoJsonContent = value;
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}