using System.Windows;

namespace PointlessWaymarks.WpfCommon.Utility;

public class BindingProxy : Freezable
{
    // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

    public object Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }
    //There are a number of scenarios in WPF where it becomes obscure how to access a different DataContext -
    //in some cases an elegant solution is to give your top level control or window a x:Name and reference
    //that as the Element= in the binding, but this doesn't appear to work in all cases.
    //
    //This class is an absolute classic and Thomas Levesque (of MVVM Light fame) is the person who I
    //associate this concept with, although I don't know the history of this code for WPF.
    //
    //This version of the code comes from:
    // https://thomaslevesque.com/2011/03/21/wpf-how-to-bind-to-data-when-the-datacontext-is-not-inherited/
    //
    //To use this the proxy must be set in resources
    //<DataGrid.Resources>
    //  <local:BindingProxy x:Key="proxy" Data="{Binding}" />
    //</DataGrid.Resources>
    //
    //And a usage example:
    //<DataGridTextColumn Header="Price" Binding="{Binding Price}" IsReadOnly="False"
    //  Visibility="{Binding Data.ShowPrice, Converter={StaticResource visibilityConverter}, Source={StaticResource proxy}}"/>
    //

    protected override Freezable CreateInstanceCore()
    {
        return new BindingProxy();
    }
}