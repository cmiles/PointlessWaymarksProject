using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.SearchBuilder;

public class FilterTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ContentSearchTemplate { get; set; }
    public DataTemplate? NumericSearchTemplate { get; set; }
    public DataTemplate? TextSearchTemplate { get; set; }


    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        return item switch
        {
            TextSearchFieldBuilder => TextSearchTemplate,
            NumericSearchFieldBuilder => NumericSearchTemplate,
            ContentTypeSearchBuilder => ContentSearchTemplate,
            _ => null
        };
    }
}