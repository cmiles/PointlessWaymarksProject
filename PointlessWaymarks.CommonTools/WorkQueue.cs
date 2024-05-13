using System.Collections.Concurrent;
using System.Diagnostics;
using Serilog;

namespace PointlessWaymarks.CommonTools;

public class WorkQueue<T>
{
    //This is basically the BlockingCollection version from https://michaelscodingspot.com/c-job-queues/
    private readonly BlockingCollection<(DateTime created, T job)> _jobs = new();

    private readonly List<(DateTime created, T job)> _pausedQueue = [];

    private bool _suspended;

    public WorkQueue(bool suspended = false)
    {
        _suspended = suspended;
        var thread = new Thread(OnStart) {IsBackground = true};
        thread.Start();
    }

    public Func<T, Task>? Processor { get; set; }

    public void Enqueue(T job)
    {
        if (_suspended) _pausedQueue.Add((DateTime.Now,  job));
        else _jobs.Add((DateTime.Now, job));
    }

    private void OnStart()
    {
        foreach (var job in _jobs.GetConsumingEnumerable(CancellationToken.None))
            try
            {
                if (_suspended)
                {
                    _pausedQueue.Add(job);
                }
                else
                {
                    Processor?.Invoke(job.job).Wait();
                }
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
        if (!_suspended && _pausedQueue.Count != 0)
        {
            _pausedQueue.OrderBy(x => x.created).ToList().ForEach(x => _jobs.Add(x));
            _pausedQueue.Clear();
        }
    }
}