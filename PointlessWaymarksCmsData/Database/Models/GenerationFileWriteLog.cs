using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class GenerationFileWriteLog
    {
        public int Id { get; set; }
        public DateTime WrittenOnVersion { get; set; }
        public string FileName { get; set; }
    }
}
