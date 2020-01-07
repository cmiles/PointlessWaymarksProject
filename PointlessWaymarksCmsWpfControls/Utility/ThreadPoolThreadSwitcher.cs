using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public struct ThreadPoolThreadSwitcher : INotifyCompletion
    {
        public bool IsCompleted => SynchronizationContext.Current == null;

        public ThreadPoolThreadSwitcher GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
        }

        public void OnCompleted(Action continuation)
        {
            ThreadPool.QueueUserWorkItem(_ => continuation());
        }
    }
}