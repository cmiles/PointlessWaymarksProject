using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointlessWaymarks.CmsData
{
    public class ConsoleProgress : IProgress<string>
    {
        public void Report(string value)
        {
            Console.WriteLine(value);
        }
    }
}
