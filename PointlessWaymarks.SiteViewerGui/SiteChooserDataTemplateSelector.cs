using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.SiteViewerGui;

public class SiteChooserDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SiteDirectoryTemplate { get; set; }
    public DataTemplate? SiteSettingsFileTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            SiteSettingsFileListItem => SiteSettingsFileTemplate,
            SiteDirectoryListItem => SiteDirectoryTemplate,
            _ => null
        };
    }
}