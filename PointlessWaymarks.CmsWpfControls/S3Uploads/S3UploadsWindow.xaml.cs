#nullable enable
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.S3Uploads
{
    /// <summary>
    ///     Interaction logic for S3UploadsWindow.xaml
    /// </summary>
    public partial class S3UploadsWindow : INotifyPropertyChanged
    {
        private bool _forceClose;
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

        private void S3UploadsWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (_forceClose) return;

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(WindowCloseOverload);
            e.Cancel = true;
        }

        public async Task WindowCloseOverload()
        {
            if (UploadContext?.UploadBatch == null || !UploadContext.UploadBatch.Uploading)
            {
                _forceClose = true;
                await ThreadSwitcher.ResumeForegroundAsync();
                Close();
            }

            var userAction = await StatusContext.ShowMessage("Running Upload...",
                "Exiting this window with an upload running could create errors on S3:",
                new List<string> {"Close Immediately", "Cancel and Close", "Return to Upload"});

            switch (userAction)
            {
                case "Close Immediately":
                {
                    _forceClose = true;
                    await ThreadSwitcher.ResumeForegroundAsync();
                    Close();
                    break;
                }
                case "Return and Cancel":
                {
                    UploadContext?.UploadBatch?.Cancellation?.Cancel();
                    break;
                }
            }
        }
    }
}