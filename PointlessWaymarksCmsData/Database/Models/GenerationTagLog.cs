using System;
using System.Collections.Generic;
using System.Text;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class GenerationTagLog
    {
        public int Id { get; set; }
        public string TagSlug { get; set; }
        public bool TagIsExcludedFromSearch { get; set; }
        public DateTime GenerationVersion { get; set; }
        public Guid RelatedContentId { get; set; }
    }
}
