using System;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace TheLemmonWorkshopWpfControls.Utility
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
            return this;
        }

        public void GetResult()
        {
        }

        public void OnCompleted(Action continuation)
        {
            _dispatcher.BeginInvoke(continuation);
        }
    }
}