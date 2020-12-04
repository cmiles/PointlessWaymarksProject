using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class EventLog
    {
        public string Category { get; set; }
        public int Id { get; set; }
        public string Information { get; set; }
        public DateTime RecordedOn { get; set; }
        public string Sender { get; set; }
    }
}