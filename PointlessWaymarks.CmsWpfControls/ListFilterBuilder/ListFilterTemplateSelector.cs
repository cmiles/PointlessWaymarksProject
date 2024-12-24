using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.ListFilterBuilder;

public class ListFilterTemplateSelector : DataTemplateSelector
{
    public DataTemplate? BooleanListFilterTemplate { get; set; }
    public DataTemplate? BoundsListFilterTemplate { get; set; }
    public DataTemplate? ContentIdListFilterTemplate { get; set; }
    public DataTemplate? ContentTypeListFilterTemplate { get; set; }
    public DataTemplate? DateListFilterTemplate { get; set; }
    public DataTemplate? NumericListFilterTemplate { get; set; }
    public DataTemplate? TextListFilterTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        return item switch
        {
            TextListFilterFieldBuilder => TextListFilterTemplate,
            NumericListFilterFieldBuilder => NumericListFilterTemplate,
            ContentTypeListFilterFieldBuilder => ContentTypeListFilterTemplate,
            DateTimeListFilterFieldBuilder => DateListFilterTemplate,
            BooleanListFilterFieldBuilder => BooleanListFilterTemplate,
            BoundsListFilterFieldBuilder => BoundsListFilterTemplate,
            ContentIdListFilterFieldBuilder => ContentIdListFilterTemplate,
            _ => null
        };
    }
}