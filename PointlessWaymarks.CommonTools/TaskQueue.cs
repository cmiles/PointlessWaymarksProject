using System.Collections.Concurrent;
using System.Diagnostics;
using Serilog;

namespace PointlessWaymarks.CommonTools;

public class TaskQueue
{
    //This is basically the BlockingCollection version from https://michaelscodingspot.com/c-job-queues/
    private readonly BlockingCollection<Func<Task>> _jobs = new();

    private readonly List<Func<Task>> _pausedQueue = [];

    private bool _suspended;

    public TaskQueue(bool suspended = false)
    {
        _suspended = suspended;
        var thread = new Thread(OnStart) {IsBackground = true};
        thread.Start();
    }

    public void Enqueue(Func<Task> job)
    {
        if (_suspended) _pausedQueue.Add(job);
        else _jobs.Add(job);
    }

    private void OnStart()
    {
        foreach (var job in _jobs.GetConsumingEnumerable(CancellationToken.None))
            try
            {
                job.Invoke().Wait();
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                Log.Error(e, "WorkQueue Error");
            }
    }

    public void Suspend(bool suspend)
    {
        _suspended = suspend;
        if (!_suspended && _pausedQueue.Any()) _pausedQueue.ForEach(x => _jobs.Add(x));
    }
}