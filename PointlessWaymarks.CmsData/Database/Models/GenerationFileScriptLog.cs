using System;

namespace PointlessWaymarks.CmsData.Database.Models
{
    public class GenerationFileScriptLog
    {
        public string FileName { get; set; }
        public int Id { get; set; }
        public DateTime WrittenOnVersion { get; set; }
    }
}