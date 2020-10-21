using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class GenerationFileScriptLog
    {
        public int Id { get; set; }
        public DateTime WrittenOnVersion { get; set; }
        public string FileName { get; set; }
    }
}