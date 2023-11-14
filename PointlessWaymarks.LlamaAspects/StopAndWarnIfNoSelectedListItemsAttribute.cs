using Metalama.Framework.Aspects;

namespace PointlessWaymarks.LlamaAspects;

public class StopAndWarnIfNoSelectedListItemsAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InsertStatement("await ThreadSwitcher.ResumeBackgroundAsync();");

        if (!meta.This.SelectedListItems().Any())
        {
            meta.This.StatusContext.ToastError("Nothing Selected?");
            meta.Return();
        }

        return meta.Proceed();
    }
}