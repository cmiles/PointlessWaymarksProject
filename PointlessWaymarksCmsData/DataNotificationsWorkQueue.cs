using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database;
using TinyIpc.Messaging;

namespace PointlessWaymarksCmsData
{
    /// <summary>
    ///     This Queue takes in DataNotifications and processes them one at a time.
    /// </summary>
    public class DataNotificationsWorkQueue
    {
        //This is basically the BlockingCollection version from https://michaelscodingspot.com/c-job-queues/
        private readonly BlockingCollection<TinyMessageReceivedEventArgs> _jobs =
            new BlockingCollection<TinyMessageReceivedEventArgs>();

        public DataNotificationsWorkQueue()
        {
            var thread = new Thread(OnStart) {IsBackground = true};
            thread.Start();
        }

        public Func<TinyMessageReceivedEventArgs, Task> Processor { get; set; }

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
                    EventLogContext.TryWriteExceptionToLogBlocking(e,
                        $"DataNotificationsWorkQueue",
                        string.Empty);
                }
        }
    }
}