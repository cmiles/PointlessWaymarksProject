using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

public class ProgressDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ErrorTemplate { get; set; }
    public DataTemplate? ProgressTemplate { get; set; }
    public DataTemplate? StateTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        return item switch
        {
            ScriptMessageItemProgress => ProgressTemplate,
            ScriptMessageItemState => StateTemplate,
            ScriptMessageItemError => ErrorTemplate,
            _ => null
        };
    }
}