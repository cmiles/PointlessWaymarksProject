using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class GenerationDailyPhotoLog
    {
        public DateTime DailyPhotoDate { get; set; }
        public DateTime GenerationVersion { get; set; }
        public int Id { get; set; }
        public Guid RelatedContentId { get; set; }
    }
}