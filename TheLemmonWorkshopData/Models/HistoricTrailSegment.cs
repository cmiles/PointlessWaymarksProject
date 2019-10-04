using NetTopologySuite.Geometries;
using System;

namespace TheLemmonWorkshopData.Models
{
    public class HistoricTrailSegment
    {
        public string Code { get; set; }
        public decimal CumulativeElevation { get; set; }
        public decimal ElevationGain { get; set; }
        public decimal ElevationLoss { get; set; }
        public string EndContent { get; set; }
        public Guid Fingerprint { get; set; }
        public decimal HighestElevation { get; set; }
        public int Id { get; set; }
        public decimal LengthInMeters { get; set; }
        public Geometry LocationData { get; set; }
        public decimal LowestElevation { get; set; }
        public string SegmentContent { get; set; }
        public string StartContent { get; set; }
    }
}