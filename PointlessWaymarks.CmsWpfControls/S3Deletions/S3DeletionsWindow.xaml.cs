using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsWpfControls.Status;

namespace PointlessWaymarks.CmsWpfControls.S3Deletions
{
    /// <summary>
    ///     Interaction logic for S3DeletionsWindow.xaml
    /// </summary>
    public partial class S3DeletionsWindow : INotifyPropertyChanged
    {
        private S3DeletionsContext _deletionContext;
        private StatusControlContext _statusContext;

        public S3DeletionsWindow(List<S3DeletionsItem> itemsToDelete)
        {
            InitializeComponent();

            _statusContext = new StatusControlContext();

            DataContext = this;

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(async () =>
            {
                DeletionContext = await S3DeletionsContext.CreateInstance(StatusContext, itemsToDelete);
            });
        }

        public S3DeletionsContext DeletionContext
        {
            get => _deletionContext;
            set
            {
                if (Equals(value, _deletionContext)) return;
                _deletionContext = value;
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

        private void S3DeletionsWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (DeletionContext == null || !StatusContext.BlockUi) return;

            e.Cancel = true;
        }
    }
}