using System;
using System.Collections.Generic;
using System.IO;
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
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.VideoContentEditor
{
    [ObservableObject]
    public partial class VideoContentEditorWindow
    {
        [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
        [ObservableProperty] private VideoContentEditorContext _videoContent;
        [ObservableProperty] private StatusControlContext _statusContext;

        /// <summary>
        ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
        ///     core functionality being uninitialized.
        /// </summary>
        private VideoContentEditorWindow()
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            DataContext = this;
        }

        /// <summary>
        ///     Creates a new instance - this method can be called from any thread and will
        ///     switch to the UI thread as needed.
        /// </summary>
        /// <returns></returns>
        public static async Task<VideoContentEditorWindow> CreateInstance()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var window = new VideoContentEditorWindow();

            await ThreadSwitcher.ResumeBackgroundAsync();

            window.VideoContent = await VideoContentEditorContext.CreateInstance(window.StatusContext);

            window.VideoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

            window.AccidentalCloserHelper =
                new WindowAccidentalClosureHelper(window, window.StatusContext, window.VideoContent)
                {
                    CloseAction = x => { ((VideoContentEditorWindow)x).VideoContent.MainImageExternalEditorWindowCleanup(); }
                };

            return window;
        }

        /// <summary>
        ///     Creates a new instance - this method can be called from any thread and will
        ///     switch to the UI thread as needed.
        /// </summary>
        /// <returns></returns>
        public static async Task<VideoContentEditorWindow> CreateInstance(FileInfo initialFile)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var window = new VideoContentEditorWindow();

            await ThreadSwitcher.ResumeBackgroundAsync();

            window.VideoContent = await VideoContentEditorContext.CreateInstance(window.StatusContext, initialFile);

            window.VideoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

            window.AccidentalCloserHelper =
                new WindowAccidentalClosureHelper(window, window.StatusContext, window.VideoContent)
                {
                    CloseAction = x => { ((VideoContentEditorWindow)x).VideoContent.MainImageExternalEditorWindowCleanup(); }
                };

            return window;
        }

        /// <summary>
        ///     Creates a new instance - this method can be called from any thread and will
        ///     switch to the UI thread as needed. Does not show the window - consider using
        ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
        /// </summary>
        /// <returns></returns>
        public static async Task<VideoContentEditorWindow> CreateInstance(VideoContent toLoad)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var window = new VideoContentEditorWindow();

            await ThreadSwitcher.ResumeBackgroundAsync();

            window.VideoContent = await VideoContentEditorContext.CreateInstance(window.StatusContext, toLoad);

            window.VideoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

            window.AccidentalCloserHelper =
                new WindowAccidentalClosureHelper(window, window.StatusContext, window.VideoContent)
                {
                    CloseAction = x => { ((VideoContentEditorWindow)x).VideoContent.MainImageExternalEditorWindowCleanup(); }
                };

            await ThreadSwitcher.ResumeForegroundAsync();

            return window;
        }
    }
}
