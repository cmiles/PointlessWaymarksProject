using Omu.ValueInjecter;
using Omu.ValueInjecter.Injections;

namespace PointlessWaymarks.CmsTests;

public static class UtilityHelpers
{
    public static void InjectFromSkippingIds(this object toInject, object from)
    {
        toInject.InjectFrom(new LoopInjection(["ContentId", "Id", "ContentVersion"]), from);
    }
}