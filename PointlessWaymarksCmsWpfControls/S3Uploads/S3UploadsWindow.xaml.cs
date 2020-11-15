#nullable enable
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.S3Uploads
{
    /// <summary>
    ///     Interaction logic for S3UploadsWindow.xaml
    /// </summary>
    public partial class S3UploadsWindow : INotifyPropertyChanged
    {
        private StatusControlContext _statusContext;
        private S3UploadsContext? _uploadContext;

        public S3UploadsWindow(List<S3Upload> toLoad)
        {
            InitializeComponent();

            _statusContext = new StatusControlContext();

            DataContext = this;

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(async () =>
            {
                UploadContext = await S3UploadsContext.CreateInstance(StatusContext, toLoad);
            });
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

        public S3UploadsContext? UploadContext
        {
            get => _uploadContext;
            set
            {
                if (Equals(value, _uploadContext)) return;
                _uploadContext = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}