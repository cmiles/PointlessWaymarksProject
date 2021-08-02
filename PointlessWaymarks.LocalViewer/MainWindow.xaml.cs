using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Diagnostics;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsWpfControls.SitePreview;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.LocalViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private StatusControlContext _statusContext;
        private SitePreviewContext _previewContext;

        public MainWindow()
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();

            DataContext = this;
        }

        public MainWindow(string siteUrl, string localFolder, string siteName)
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();

            DataContext = this;

            PreviewContext = new SitePreviewContext(siteUrl,
                localFolder,
                siteName, StatusContext);
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
