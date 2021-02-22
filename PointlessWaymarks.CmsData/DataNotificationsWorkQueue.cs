using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsData
{
    /// <summary>
    ///     This Queue takes in DataNotifications and processes them one at a time.
    /// </summary>
    public class DataNotificationsWorkQueue
    {
        //This is basically the BlockingCollection version from https://michaelscodingspot.com/c-job-queues/
        private readonly BlockingCollection<TinyMessageReceivedEventArgs> _jobs = new();

        public DataNotificationsWorkQueue()
        {
            var thread = new Thread(OnStart) {IsBackground = true};
            thread.Start();
        }

        public Func<TinyMessageReceivedEventArgs, Task>? Processor { get; init; }

        public void Enqueue(TinyMessageReceivedEventArgs job)
        {
            _jobs.Add(job);
        }

        private void OnStart()
        {
            foreach (var job in _jobs.GetConsumingEnumerable(CancellationToken.None))
                try
                {
                    Processor?.Invoke(job).Wait();
                }
                catch (Exception e)
                {
                    Debug.Print(e.Message);
                    Log.Error(e, "DataNotificationsWorkQueue OnStart Error");
                }
        }
    }
}