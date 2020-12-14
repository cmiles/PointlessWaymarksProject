using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class GenerationFileWriteLog
    {
        public string FileName { get; set; }
        public int Id { get; set; }
        public DateTime WrittenOnVersion { get; set; }
    }
}