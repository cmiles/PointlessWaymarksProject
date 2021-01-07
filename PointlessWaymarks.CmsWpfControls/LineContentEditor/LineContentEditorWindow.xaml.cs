using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Status;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.CmsWpfControls.Utility.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.LineContentEditor
{
    /// <summary>
    ///     Interaction logic for LineContentEditorWindow.xaml
    /// </summary>
    public partial class LineContentEditorWindow : INotifyPropertyChanged
    {
        private LineContentEditorContext _postContent;
        private StatusControlContext _statusContext;

        public LineContentEditorWindow(LineContent toLoad)
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(async () =>
            {
                LineContent = await LineContentEditorContext.CreateInstance(StatusContext, toLoad);

                LineContent.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
                AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, LineContent);

                await ThreadSwitcher.ResumeForegroundAsync();
                DataContext = this;
            });
        }

        public WindowAccidentalClosureHelper AccidentalCloserHelper { get; set; }

        public LineContentEditorContext LineContent
        {
            get => _postContent;
            set
            {
                if (Equals(value, _postContent)) return;
                _postContent = value;
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