using Metalama.Framework.Aspects;

namespace PointlessWaymarks.LlamaAspects;

public class StopAndWarnIfNoOrMoreThanSelectedItemsAttribute : OverrideMethodAspect
{
    public int MaxSelectedItems { get; set;  }

    public override dynamic? OverrideMethod()
    {
        meta.InsertStatement("await ThreadSwitcher.ResumeBackgroundAsync();");

        if (!meta.This.SelectedItems().Any())
        {
            meta.This.StatusContext.ToastError("Nothing Selected?");
            meta.Return();
        }

        if (meta.This.SelectedItems().Count >= this.MaxSelectedItems)
        {
            meta.This.StatusContext.ToastError($"Please select less than {this.MaxSelectedItems} Items");
            meta.Return();
        }

        return meta.Proceed();
    }
}