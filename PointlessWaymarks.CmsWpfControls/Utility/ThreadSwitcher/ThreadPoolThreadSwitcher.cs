﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PointlessWaymarks.CmsWpfControls.Utility.ThreadSwitcher
{
    public struct ThreadPoolThreadSwitcher : INotifyCompletion
    {
        public bool IsCompleted => SynchronizationContext.Current == null;

        public ThreadPoolThreadSwitcher GetAwaiter()
        {
            //Debug.Print($"ThreadPoolThreadSwitcher GetAwaiter from {Thread.CurrentThread.ManagedThreadId}");
            return this;
        }

        public void GetResult()
        {
        }

        public void OnCompleted(Action continuation)
        {
            //Debug.Print($"ThreadPoolThreadSwitcher OnCompleted from {Thread.CurrentThread.ManagedThreadId}");
            ThreadPool.QueueUserWorkItem(_ => continuation());
        }
    }
}