using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsWpfControls.Status;
using TinyIpc.Messaging;

namespace PointlessWaymarksCmsWpfControls.Utility
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

        public StatusControlContext StatusContent { get; set; }

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
                    EventLogContext.TryWriteExceptionToLogBlocking(e,
                        $"DataNotificationWorkQueue - Status Context Id: {StatusContent?.StatusControlContextId}",
                        string.Empty);
                    StatusContent?.ToastWarning("Trouble merging updates - item(s) may be out of date?");
                }
        }
    }
}