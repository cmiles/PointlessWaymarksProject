using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class GenerationLog
    {
        public string GenerationSettings { get; set; }
        public DateTime GenerationVersion { get; set; }
        public int Id { get; set; }
    }
}