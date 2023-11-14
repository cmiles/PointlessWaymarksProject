using Metalama.Framework.Aspects;

namespace PointlessWaymarks.LlamaAspects;

public class StopAndWarnIfNotOneSelectedListItemsAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InsertStatement("await ThreadSwitcher.ResumeBackgroundAsync();");

        if (!meta.This.SelectedListItems().Any())
        {
            meta.This.StatusContext.ToastError("Nothing Selected?");
            meta.Return();
        }

        if (meta.This.SelectedListItems().Count > 1)
        {
            meta.This.StatusContext.ToastError("Please select only 1 item...");
            meta.Return();
        }

        return meta.Proceed();
    }
}