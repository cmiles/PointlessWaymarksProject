using System;

namespace PointlessWaymarks.CmsData.Database.Models
{
    public class GenerationFileWriteLog
    {
        public string? FileName { get; set; }
        public int Id { get; set; }
        public DateTime WrittenOnVersion { get; set; }
    }
}