using Metalama.Framework.Aspects;
using System.Globalization;

namespace PointlessWaymarks.LlamaAspects;

public class StopAndWarnIfNoSelectedItemsAskIfOverMaxAttribute : OverrideMethodAspect
{
    public int MaxSelectedItems { get; set; }
    public string ActionVerb { get; set; } = "act on";

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

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        meta.InsertStatement("await ThreadSwitcher.ResumeBackgroundAsync();");

        if (!meta.This.SelectedItems().Any())
        {
            meta.This.StatusContext.ToastError("Nothing Selected?");
            meta.Return();
        }

        List<string> titleList = new();

        dynamic frozenSelected = meta.This.SelectedItems();

        for (var i = 0; i < frozenSelected.Count; i++)
        {
            titleList.Add(frozenSelected[i].Content().Title);
        }

        if (frozenSelected.Count >= this.MaxSelectedItems)
            if (await meta.This.StatusContext.ShowMessage($"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(this.ActionVerb.ToLowerInvariant())} Multiple Items",
                    $"You are about to {this.ActionVerb.ToLowerInvariant()} {frozenSelected.Count} items - do you really want to {this.ActionVerb.ToLowerInvariant()} all of these items?{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, titleList)}",
                    new List<string> { "Yes", "No" }) == "No")
                meta.Return();

        return await meta.ProceedAsync();
    }
}