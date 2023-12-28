using System.Collections.Concurrent;
using System.Diagnostics;
using Serilog;

namespace PointlessWaymarks.CommonTools;

/// <summary>
///     This Queue takes in DataNotifications and processes them one at a time.
/// </summary>
public class OneAtATimeWorkQueue<T>
{
    //This is basically the BlockingCollection version from https://michaelscodingspot.com/c-job-queues/
    private readonly BlockingCollection<T> _jobs = new();
    private readonly object _startLock = new();
    private bool _started = false;
    private bool _stopRequested;
    private Thread? _thread;

    public OneAtATimeWorkQueue(bool startImmediately = false)
    {
        if (startImmediately) Start();
    }

    public Func<T, Task>? Processor { get; set; }

    public void Enqueue(T data)
    {
        _jobs.Add(data);
    }

    private async void OnStart()
    {
        foreach (var job in _jobs.GetConsumingEnumerable(CancellationToken.None))
            try
            {
                if (Processor != null) await Processor.Invoke(job);
                if (_stopRequested) return;
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                Log.Error(e, "GeneralWorkQueue OnStart Error");
            }
    }

    public void Start()
    {
        _stopRequested = false;

        lock (_startLock)
        {
            if (_started) return;
            _started = true;
            _thread = new Thread(OnStart) { IsBackground = true };
        }

        _thread.Start();
    }

    public void Stop()
    {
        _stopRequested = true;
    }
}