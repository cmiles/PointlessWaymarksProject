using Omu.ValueInjecter;
using Omu.ValueInjecter.Injections;

namespace PointlessWaymarks.CmsTests
{
    public static class UtilityHelpers
    {
        public static void InjectFromSkippingIds(this object toInject, object from)
        {
            toInject.InjectFrom(new LoopInjection(new[] {"ContentId", "Id", "ContentVersion"}), from);
        }
    }
}