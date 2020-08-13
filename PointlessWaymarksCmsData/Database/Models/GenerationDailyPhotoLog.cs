using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class GenerationDailyPhotoLog
    {
        public int Id { get; set; }
        public DateTime DailyPhotoDate { get; set; }
        public DateTime GenerationVersion { get; set; }
        public Guid RelatedContentId { get; set; }
    }
}