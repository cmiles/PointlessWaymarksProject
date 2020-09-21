using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omu.ValueInjecter;
using Omu.ValueInjecter.Injections;

namespace PointlessWaymarksTests
{
    public static class UtilityHelpers
    {
        public static void InjectFromSkippingIds(this object toInject, object from)
        {
            toInject.InjectFrom(new LoopInjection(new[] {"ContentId", "Id", "ContentVersion"}), from);
        }
    }
}
