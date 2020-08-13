using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class GenerationLog
    {
        public DateTime GenerationVersion { get; set; }
        public string GenerationSettings { get; set; }
        public int Id { get; set; }
    }
}