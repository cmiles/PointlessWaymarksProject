using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace PointlessWaymarks.WpfCommon.Behaviors;

/// <summary>
///     A sync behaviour for a MultiSelector from https://github.com/itsChris/WpfMvvmDataGridMultiselect
/// </summary>
public static class MultiSelectBehavior
{
    public static readonly DependencyProperty SynchronizedSelectedItems = DependencyProperty.RegisterAttached(
        "SynchronizedSelectedItems", typeof(IList), typeof(MultiSelectBehavior),
        new PropertyMetadata(null, OnSynchronizedSelectedItemsChanged));

    private static readonly DependencyProperty SynchronizationManagerProperty = DependencyProperty.RegisterAttached(
        "SynchronizationManager", typeof(MultiSelectSynchronizationManager), typeof(MultiSelectBehavior),
        new PropertyMetadata(null));

    private static MultiSelectSynchronizationManager? GetSynchronizationManager(DependencyObject dependencyObject)
    {
        return (MultiSelectSynchronizationManager)dependencyObject.GetValue(SynchronizationManagerProperty);
    }

    /// <summary>
    ///     Gets the synchronized selected items.
    /// </summary>
    /// <param name="dependencyObject">The dependency object.</param>
    /// <returns>The list that is acting as the sync list.</returns>
    public static IList? GetSynchronizedSelectedItems(DependencyObject dependencyObject)
    {
        return (IList)dependencyObject.GetValue(SynchronizedSelectedItems);
    }

    private static void OnSynchronizedSelectedItemsChanged(DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue != null)
        {
            var synchronizer = GetSynchronizationManager(dependencyObject);
            Debug.Assert(synchronizer != null, nameof(synchronizer) + " != null");
            
            synchronizer.StopSynchronizing();

            SetSynchronizationManager(dependencyObject, null);
        }

        // check that this property is an IList, and that it is being set on a ListBox
        if (e.NewValue is IList && dependencyObject is Selector selector)
        {
            var synchronizer = GetSynchronizationManager(dependencyObject);
            if (synchronizer == null)
            {
                synchronizer = new MultiSelectSynchronizationManager(selector);
                SetSynchronizationManager(dependencyObject, synchronizer);
            }

            synchronizer.StartSynchronizingList();
        }
    }

    private static void SetSynchronizationManager(DependencyObject dependencyObject,
        MultiSelectSynchronizationManager? value)
    {
        dependencyObject.SetValue(SynchronizationManagerProperty, value);
    }

    /// <summary>
    ///     Sets the synchronized selected items.
    /// </summary>
    /// <param name="dependencyObject">The dependency object.</param>
    /// <param name="value">The value to be set as synchronized items.</param>
    public static void SetSynchronizedSelectedItems(DependencyObject dependencyObject, IList value)
    {
        dependencyObject.SetValue(SynchronizedSelectedItems, value);
    }
}