using System.Windows;
using System.Windows.Controls;
using PointlessWaymarks.PowerShellRunnerData;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

public class ScriptJobRunViewerEditorTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CsEditorTemplate { get; set; }
    public DataTemplate? PowerShellEditorTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is ScriptJobRunViewerContext { RunView: not null } context)
            return context.RunView.ScriptType switch
            {
                ScriptKind.PowerShell => PowerShellEditorTemplate,
                ScriptKind.CsScript => CsEditorTemplate,
                _ => PowerShellEditorTemplate
            };

        return base.SelectTemplate(item, container);
    }
}