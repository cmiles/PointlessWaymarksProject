using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CloudBackupGui.Controls
{
    /// <summary>
    /// Interaction logic for JobEditorWindow.xaml
    /// </summary>
    [NotifyPropertyChanged]
    public partial class JobEditorWindow : Window
    {
        private JobEditorWindow()
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            DataContext = this;
        }

        public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
        public JobEditorContext? JobContext { get; set; }
        public StatusControlContext StatusContext { get; set; }

        /// <summary>
        ///     Creates a new instance - this method can be called from any thread and will
        ///     switch to the UI thread as needed. Does not show the window - consider using
        ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
        /// </summary>
        /// <returns></returns>
        public static async Task<JobEditorWindow> CreateInstance(BackupJob toLoad, string databaseFile)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var window = new JobEditorWindow();

            await ThreadSwitcher.ResumeBackgroundAsync();

            window.JobContext = await JobEditorContext.CreateInstance(window.StatusContext, toLoad, databaseFile);

            window.JobContext.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

            window.AccidentalCloserHelper =
                new WindowAccidentalClosureHelper(window, window.StatusContext, window.JobContext);

            await ThreadSwitcher.ResumeForegroundAsync();

            return window;
        }
    }
}
