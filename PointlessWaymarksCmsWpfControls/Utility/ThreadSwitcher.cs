using System.Windows;
using System.Windows.Threading;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public class ThreadSwitcher
    {
        // For both WPF and Windows Forms
        public static ThreadPoolThreadSwitcher ResumeBackgroundAsync()
        {
            return new ThreadPoolThreadSwitcher();
        }

        // For WPF
        public static DispatcherThreadSwitcher ResumeForegroundAsync(Dispatcher dispatcher)
        {
            return new DispatcherThreadSwitcher(dispatcher);
        }

        public static DispatcherThreadSwitcher ResumeForegroundAsync()
        {
            return new DispatcherThreadSwitcher(Application.Current.Dispatcher);
        }
    }
}