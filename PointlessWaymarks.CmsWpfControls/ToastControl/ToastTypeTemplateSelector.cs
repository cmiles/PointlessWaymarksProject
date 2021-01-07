using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.ToastControl
{
    internal class ToastTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ErrorTemplate { get; set; }
        public DataTemplate InformationTemplate { get; set; }
        public DataTemplate SuccessTemplate { get; set; }
        public DataTemplate WarningTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is ToastViewModel toastViewModel))
                return null;

            switch (toastViewModel.Type)
            {
                case ToastType.Information:
                    return InformationTemplate;

                case ToastType.Success:
                    return SuccessTemplate;

                case ToastType.Warning:
                    return WarningTemplate;

                case ToastType.Error:
                    return ErrorTemplate;

                default:
                    return null;
            }
        }
    }
}