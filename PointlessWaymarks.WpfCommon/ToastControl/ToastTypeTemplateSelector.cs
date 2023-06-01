using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.WpfCommon.ToastControl;

internal class ToastTypeTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ErrorTemplate { get; set; }
    public DataTemplate? InformationTemplate { get; set; }
    public DataTemplate? SuccessTemplate { get; set; }
    public DataTemplate? WarningTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is not ToastContext toastViewModel)
            return null;

        return toastViewModel.Type switch
        {
            ToastType.Information => InformationTemplate,
            ToastType.Success => SuccessTemplate,
            ToastType.Warning => WarningTemplate,
            ToastType.Error => ErrorTemplate,
            _ => null
        };
    }
}