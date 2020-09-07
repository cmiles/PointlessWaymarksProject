using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database;

namespace PointlessWaymarksCmsData
{
    public class WorkQueue<T>
    {
        //This is basically the BlockingCollection version from https://michaelscodingspot.com/c-job-queues/
        private readonly BlockingCollection<T> _jobs =
            new BlockingCollection<T>();

        public WorkQueue()
        {
            var thread = new Thread(OnStart) { IsBackground = true };
            thread.Start();
        }

        public Func<T, Task> Processor { get; set; }

        public void Enqueue(T job)
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
                    EventLogContext.TryWriteExceptionToLogBlocking(e,
                        $"WorkQueue",
                        string.Empty);
                }
        }
    }
}