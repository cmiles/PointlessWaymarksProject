using System.Windows;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    //https://www.thomaslevesque.com/2011/03/21/wpf-how-to-bind-to-data-when-the-datacontext-is-not-inherited/
    //
    //<DataGrid.Resources>
    //<local:BindingProxy x:Key="Proxy" Data="{Binding}" />
    //</DataGrid.Resources>
    //
    //Visibility="{Binding Data.ShowPrice,
    //    Converter={StaticResource visibilityConverter},
    //    Source={StaticResource Proxy}}"
    public class BindingProxy : Freezable
    {
        // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

        public object Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }
    }
}