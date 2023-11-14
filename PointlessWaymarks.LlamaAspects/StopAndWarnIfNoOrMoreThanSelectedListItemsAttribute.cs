using Metalama.Framework.Aspects;

namespace PointlessWaymarks.LlamaAspects;

public class StopAndWarnIfNoOrMoreThanSelectedListItemsAttribute : OverrideMethodAspect
{
    public int MaxSelectedItems { get; set;  }

    public override dynamic? OverrideMethod()
    {
        meta.InsertStatement("await ThreadSwitcher.ResumeBackgroundAsync();");

        if (!meta.This.SelectedListItems().Any())
        {
            meta.This.StatusContext.ToastError("Nothing Selected?");
            meta.Return();
        }

        if (meta.This.SelectedListItems().Count >= this.MaxSelectedItems)
        {
            meta.This.StatusContext.ToastError($"Please select less than {this.MaxSelectedItems} Items");
            meta.Return();
        }

        return meta.Proceed();
    }
}