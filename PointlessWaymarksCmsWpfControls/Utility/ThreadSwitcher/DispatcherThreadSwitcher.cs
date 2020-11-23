using System;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace PointlessWaymarksCmsWpfControls.Utility.ThreadSwitcher
{
    public struct DispatcherThreadSwitcher : INotifyCompletion
    {
        private readonly Dispatcher _dispatcher;

        internal DispatcherThreadSwitcher(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public bool IsCompleted => _dispatcher.CheckAccess();

        public DispatcherThreadSwitcher GetAwaiter()
        {
            //Debug.Print($"DispatcherThreadSwitcher GetAwaiter from {Thread.CurrentThread.ManagedThreadId}");

            return this;
        }

        public void GetResult()
        {
        }

        public void OnCompleted(Action continuation)
        {
            //Debug.Print($"DispatcherThreadSwitcher OnCompleted from {Thread.CurrentThread.ManagedThreadId}");

            _dispatcher.BeginInvoke(continuation);
        }
    }
}