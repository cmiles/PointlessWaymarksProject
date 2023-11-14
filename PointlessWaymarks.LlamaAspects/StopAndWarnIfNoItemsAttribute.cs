using Metalama.Framework.Aspects;

namespace PointlessWaymarks.LlamaAspects;

public class StopAndWarnIfNoItemsAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InsertStatement("await ThreadSwitcher.ResumeBackgroundAsync();");

        if (!meta.This.Items.Any())
        {
            meta.This.StatusContext.ToastError("No Items?");
            meta.Return();
        }

        return meta.Proceed();
    }
}