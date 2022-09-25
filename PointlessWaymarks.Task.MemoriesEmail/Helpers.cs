using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointlessWaymarks.Task.MemoriesEmail
{
    public static class Helpers
    {
        public static string SafeObjectDump(this object toDump)
        {
            return ObjectDumper.Dump(toDump, new DumpOptions { MaxLevel = 2, DumpStyle = DumpStyle.Console });
        }
    }
}
