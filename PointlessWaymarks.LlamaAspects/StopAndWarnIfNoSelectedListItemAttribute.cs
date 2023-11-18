using Metalama.Framework.Aspects;

namespace PointlessWaymarks.LlamaAspects;

public class StopAndWarnIfNoSelectedListItemAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InsertStatement("await ThreadSwitcher.ResumeBackgroundAsync();");

        if (meta.This.SelectedListItem() == null)
        {
            meta.This.StatusContext.ToastError("Nothing Selected?");
            meta.Return();
        }

        return meta.Proceed();
    }
}