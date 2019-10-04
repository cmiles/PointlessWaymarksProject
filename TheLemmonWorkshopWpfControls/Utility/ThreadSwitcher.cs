using System.Windows;
using System.Windows.Threading;

namespace TheLemmonWorkshopWpfControls.Utility
{
    public class ThreadSwitcher
    {
        // For both WPF and Windows Forms
        public static ThreadPoolThreadSwitcher ResumeBackgroundAsync() =>
            new ThreadPoolThreadSwitcher();

        // For WPF
        public static DispatcherThreadSwitcher ResumeForegroundAsync(
            Dispatcher dispatcher) =>
            new DispatcherThreadSwitcher(dispatcher);

        public static DispatcherThreadSwitcher ResumeForegroundAsync() =>
            new DispatcherThreadSwitcher(Application.Current.Dispatcher);
    }
}