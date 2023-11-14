using System.Runtime.CompilerServices;

namespace PointlessWaymarks.WpfCommon;

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