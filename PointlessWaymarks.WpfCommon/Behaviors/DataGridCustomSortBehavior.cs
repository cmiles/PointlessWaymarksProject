using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PointlessWaymarks.WpfCommon.Behaviors;

/// <summary>
///     A behavior to allow setting up a custom DataGrid sort. See the class code for usage. Based on
///     https://stackoverflow.com/questions/18122751/wpf-datagrid-customsort-for-each-column/18218963#18218963. Basically
///     this is a way to address the scenario where your column is getting a string style sort rather than the expected
///     sort - it is VERY possible that there are other solutions including the chosen type of column, but sometimes
///     this is probably the best solution...
/// </summary>
public class DataGridCustomSortBehavior
{
    public static readonly DependencyProperty CustomSortProperty = DependencyProperty.RegisterAttached("CustomSort",
        typeof(IComparer),
        typeof(DataGridCustomSortBehavior));

    public static readonly DependencyProperty AllowCustomSortProperty = DependencyProperty.RegisterAttached(
        "AllowCustomSort", typeof(bool),
        typeof(DataGridCustomSortBehavior), new UIPropertyMetadata(false, OnAllowCustomSortChanged));

    //Basic usage:
    // - Setup a comparer to use - See the SimpleNumberFirstToStringCompare class as a generic starting point
    // - Attach the behavior to the DataGrid
    // - Assign the sorter in the columns
    // <UserControl.Resources>
    //      <converters:MyComparer x:Key="MyComparer"/>
    // </UserControl.Resources>
    // <DataGrid behaviours:CustomSortBehaviour.AllowCustomSort="True" ItemsSource="{Binding MyListCollectionView}">
    //      <DataGrid.Columns>
    //          <DataGridTextColumn Header="Test" Binding="{Binding MyValue}" behaviours:CustomSortBehaviour.CustomSorter="{StaticResource MyComparer}" />
    //      </DataGrid.Columns>
    // </DataGrid>

    public static bool ApplySort(DataGrid grid, DataGridColumn column)
    {
        var sorter = GetCustomSort(column);
        if (sorter == null) return false;

        var listCollectionView = CollectionViewSource.GetDefaultView(grid.ItemsSource) as ListCollectionView;
        if (listCollectionView == null)
            throw new Exception("The ICollectionView associated with the DataGrid must be of type, ListCollectionView");

        listCollectionView.CustomSort = new DataGridSortComparer(sorter,
            column.SortDirection ?? ListSortDirection.Ascending, column.SortMemberPath);
        return true;
    }

    public static bool GetAllowCustomSort(DataGrid grid)
    {
        return (bool)grid.GetValue(AllowCustomSortProperty);
    }

    public static IComparer? GetCustomSort(DataGridColumn column)
    {
        return (IComparer)column.GetValue(CustomSortProperty);
    }

    private static void HandleCustomSorting(object sender, DataGridSortingEventArgs e)
    {
        var sorter = GetCustomSort(e.Column);
        if (sorter == null) return;

        var grid = (DataGrid)sender;
        e.Column.SortDirection = e.Column.SortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;
        if (ApplySort(grid, e.Column)) e.Handled = true;
    }

    private static void OnAllowCustomSortChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        var grid = (DataGrid)obj;

        var oldAllow = (bool)e.OldValue;
        var newAllow = (bool)e.NewValue;

        if (!oldAllow && newAllow)
            grid.Sorting += HandleCustomSorting;
        else
            grid.Sorting -= HandleCustomSorting;
    }

    public static void SetAllowCustomSort(DataGrid grid, bool value)
    {
        grid.SetValue(AllowCustomSortProperty, value);
    }

    public static void SetCustomSort(DataGridColumn column, IComparer value)
    {
        column.SetValue(CustomSortProperty, value);
    }

    private class DataGridSortComparer(IComparer comparer, ListSortDirection sortDirection, string propertyName)
        : IComparer
    {
        private PropertyInfo? _property;

        public int Compare(object? x, object? y)
        {
            var property = _property ??= x?.GetType().GetProperty(propertyName);
            var value1 = property?.GetValue(x);
            var value2 = property?.GetValue(y);

            var result = comparer.Compare(value1, value2);
            if (sortDirection == ListSortDirection.Descending) result = -result;

            return result;
        }
    }
}