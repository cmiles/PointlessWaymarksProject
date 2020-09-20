using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    //http://stackoverflow.com/questions/2006729/how-can-i-have-a-listbox-auto-scroll-when-a-new-item-is-added
    public class ListBoxAutoScrollToBottomBehavior
    {
        public static readonly DependencyProperty ScrollOnNewItemProperty =
            DependencyProperty.RegisterAttached("ScrollOnNewItem", typeof(bool),
                typeof(ListBoxAutoScrollToBottomBehavior), new UIPropertyMetadata(false, OnScrollOnNewItemChanged));

        private static readonly Dictionary<ListBox, Capture> Associations = new Dictionary<ListBox, Capture>();

        public static bool GetScrollOnNewItem(DependencyObject obj)
        {
            return (bool) obj.GetValue(ScrollOnNewItemProperty);
        }

        public static void OnScrollOnNewItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ListBox listBox)) return;
            bool oldValue = (bool) e.OldValue, newValue = (bool) e.NewValue;
            if (newValue == oldValue) return;
            if (newValue)
            {
                listBox.Loaded += ListBoxLoaded;
                listBox.Unloaded += ListBoxUnloaded;
                var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(listBox)["ItemsSource"];
                itemsSourcePropertyDescriptor.AddValueChanged(listBox, ListBoxItemsSourceChanged);
            }
            else
            {
                listBox.Loaded -= ListBoxLoaded;
                listBox.Unloaded -= ListBoxUnloaded;
                if (Associations.ContainsKey(listBox))
                    Associations[listBox].Dispose();
                var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(listBox)["ItemsSource"];
                itemsSourcePropertyDescriptor.RemoveValueChanged(listBox, ListBoxItemsSourceChanged);
            }
        }

        public static void SetScrollOnNewItem(DependencyObject obj, bool value)
        {
            obj.SetValue(ScrollOnNewItemProperty, value);
        }

        private static void ListBoxItemsSourceChanged(object sender, EventArgs e)
        {
            var listBox = (ListBox) sender;
            if (Associations.ContainsKey(listBox))
                Associations[listBox].Dispose();
            Associations[listBox] = new Capture(listBox);
        }

        private static void ListBoxLoaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox) sender;
            //listBox.Loaded -= ListBoxLoaded;
            Associations[listBox] = new Capture(listBox);
        }

        private static void ListBoxUnloaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox) sender;
            if (Associations.ContainsKey(listBox))
                Associations[listBox].Dispose();
            //listBox.Unloaded -= ListBoxUnloaded;
        }

        private class Capture : IDisposable
        {
            private readonly INotifyCollectionChanged _incc;
            private readonly ListBox _listBox;

            public Capture(ListBox listBox)
            {
                _listBox = listBox;
                _incc = listBox.ItemsSource as INotifyCollectionChanged;
                if (_incc != null)
                {
                    _incc.CollectionChanged += InccCollectionChanged;
                }
            }

            public void Dispose()
            {
                if (_incc != null)
                    _incc.CollectionChanged -= InccCollectionChanged;
            }

            private void InccCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    try
                    {
                        Application.Current?.Dispatcher?.BeginInvoke((Action) (() =>
                        {
                            _listBox.ScrollIntoView(e.NewItems[0]);
                            _listBox.SelectedItem = e.NewItems[0];
                        }), DispatcherPriority.DataBind);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }
    }
}