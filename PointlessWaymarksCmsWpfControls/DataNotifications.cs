using System;
using System.Collections.Generic;
using WeakEvent;

namespace PointlessWaymarksCmsWpfControls
{
    public static class DataNotifications
    {

        public static readonly WeakEventSource<DataNotificationEventArgs> FileContentDataNotificationEventSource = new WeakEventSource<DataNotificationEventArgs>();
        public static event EventHandler<DataNotificationEventArgs> FileContentDataNotificationEvent
        {
            add => FileContentDataNotificationEventSource.Subscribe(value);
            remove => FileContentDataNotificationEventSource.Unsubscribe(value);
        }

        public static readonly WeakEventSource<DataNotificationEventArgs> ImageContentDataNotificationEventSource = new WeakEventSource<DataNotificationEventArgs>();
        public static event EventHandler<DataNotificationEventArgs> ImageContentDataNotificationEvent
        {
            add => ImageContentDataNotificationEventSource.Subscribe(value);
            remove => ImageContentDataNotificationEventSource.Unsubscribe(value);
        }

        public static readonly WeakEventSource<DataNotificationEventArgs> LinkStreamContentDataNotificationEventSource = new WeakEventSource<DataNotificationEventArgs>();
        public static event EventHandler<DataNotificationEventArgs> LinkStreamContentDataNotificationEvent
        {
            add => LinkStreamContentDataNotificationEventSource.Subscribe(value);
            remove => LinkStreamContentDataNotificationEventSource.Unsubscribe(value);
        }

        public static readonly WeakEventSource<DataNotificationEventArgs> NoteContentDataNotificationEventSource = new WeakEventSource<DataNotificationEventArgs>();
        public static event EventHandler<DataNotificationEventArgs> NoteContentDataNotificationEvent
        {
            add => NoteContentDataNotificationEventSource.Subscribe(value);
            remove => NoteContentDataNotificationEventSource.Unsubscribe(value);
        }

        public static readonly WeakEventSource<DataNotificationEventArgs> PhotoContentDataNotificationEventSource = new WeakEventSource<DataNotificationEventArgs>();
        public static event EventHandler<DataNotificationEventArgs> PhotoContentDataNotificationEvent
        {
            add => PhotoContentDataNotificationEventSource.Subscribe(value);
            remove => PhotoContentDataNotificationEventSource.Unsubscribe(value);
        }

        public static readonly WeakEventSource<DataNotificationEventArgs> PostContentDataNotificationEventSource = new WeakEventSource<DataNotificationEventArgs>();
        public static event EventHandler<DataNotificationEventArgs> PostContentDataNotificationEvent
        {
            add => PostContentDataNotificationEventSource.Subscribe(value);
            remove => PostContentDataNotificationEventSource.Unsubscribe(value);
        }


    }

    public enum DataNotificationUpdateType
    {
        New,
        Update,
        Delete
    }

    public class DataNotificationEventArgs : EventArgs
    {
        public DataNotificationUpdateType UpdateType { get; set; }
        public List<Guid> ContentIds { get; set; }
    }
}
