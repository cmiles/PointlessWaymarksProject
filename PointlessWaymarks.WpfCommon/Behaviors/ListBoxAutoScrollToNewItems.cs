using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;

namespace PointlessWaymarks.WpfCommon.Behaviors;

public class ListBoxAutoScrollToNewItems : Behavior<ListBox>
{
    private INotifyCollectionChanged? _cachedItemsSource;

    private void AssociatedObjectLoaded(object sender, RoutedEventArgs e)
    {
        UpdateItemsSource();
    }

    private void ItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems?[0] != null)
            try
            {
                Application.Current?.Dispatcher?.BeginInvoke((Action) (() =>
                {
                    AssociatedObject.ScrollIntoView(e.NewItems[0]!);
                    AssociatedObject.SelectedItem = e.NewItems[0];
                }), DispatcherPriority.DataBind);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        UpdateItemsSource();

        AssociatedObject.Loaded += AssociatedObjectLoaded;
    }

    private void UpdateItemsSource()
    {
        if (AssociatedObject.ItemsSource is INotifyCollectionChanged sourceCollection)
        {
            if (_cachedItemsSource != null) _cachedItemsSource.CollectionChanged -= ItemsSourceCollectionChanged;
            _cachedItemsSource = sourceCollection;
            if (_cachedItemsSource == null) return;
            _cachedItemsSource.CollectionChanged += ItemsSourceCollectionChanged;
        }
    }
}