using System.Windows;
using System.Windows.Threading;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public class ThreadSwitcher
    {
        /// <summary>
        /// If present the PinnedDispatcher will be used by ResumeForegroundAsync() (otherwise Application.Current.Dispatcher is used)
        /// </summary>
        public static Dispatcher PinnedDispatcher { get; set; }

        public static ThreadPoolThreadSwitcher ResumeBackgroundAsync()
        {
            return new ThreadPoolThreadSwitcher();
        }

        /// <summary>
        /// Uses the PinnedDispatcher if not null of Application.Current.Dispatcher
        /// </summary>
        /// <returns></returns>
        public static DispatcherThreadSwitcher ResumeForegroundAsync()
        {
            return PinnedDispatcher == null
                ? new DispatcherThreadSwitcher(Application.Current.Dispatcher)
                : new DispatcherThreadSwitcher(PinnedDispatcher);
        }
    }
}