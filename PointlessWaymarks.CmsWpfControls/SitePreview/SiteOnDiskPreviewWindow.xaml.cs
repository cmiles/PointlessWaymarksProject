using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.SitePreview
{
    /// <summary>
    ///     Interaction logic for SiteOnDiskPreviewWindow.xaml
    /// </summary>
    public partial class SiteOnDiskPreviewWindow : INotifyPropertyChanged
    {
        private SitePreviewContext _previewContext;
        private StatusControlContext _statusContext;

        public SiteOnDiskPreviewWindow()
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();

            DataContext = this;

            PreviewContext = new SitePreviewContext(UserSettingsSingleton.CurrentSettings().SiteUrl,
                UserSettingsSingleton.CurrentSettings().LocalSiteRootDirectory,
                UserSettingsSingleton.CurrentSettings().SiteName, "", StatusContext);
        }

        public SitePreviewContext PreviewContext
        {
            get => _previewContext;
            set
            {
                if (Equals(value, _previewContext)) return;
                _previewContext = value;
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