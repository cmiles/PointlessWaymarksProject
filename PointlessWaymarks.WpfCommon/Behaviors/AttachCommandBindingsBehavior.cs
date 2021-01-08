using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace PointlessWaymarks.WpfCommon.Behaviors
{
    //https://stackoverflow.com/questions/48506185/using-applicationcommands-in-wpf-prism/48506698#48506698
    public class AttachCommandBindingsBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty CommandBindingsProperty =
            DependencyProperty.Register("CommandBindings", typeof(ObservableCollection<CommandBinding>),
                typeof(AttachCommandBindingsBehavior), new PropertyMetadata(null, OnCommandBindingsChanged));

        public ObservableCollection<CommandBinding> CommandBindings
        {
            get => (ObservableCollection<CommandBinding>) GetValue(CommandBindingsProperty);
            set => SetValue(CommandBindingsProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            UpdateCommandBindings();
        }

        private static void OnCommandBindingsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
#pragma warning disable IDE0083 // Use pattern matching
            if (!(sender is AttachCommandBindingsBehavior b))
#pragma warning restore IDE0083 // Use pattern matching
                return;

            if (e.OldValue is ObservableCollection<CommandBinding> oldBindings)
                oldBindings.CollectionChanged -= b.OnCommandBindingsCollectionChanged;

            if (e.OldValue is ObservableCollection<CommandBinding> newBindings)
                newBindings.CollectionChanged += b.OnCommandBindingsCollectionChanged;

            b.UpdateCommandBindings();
        }

        private void OnCommandBindingsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCommandBindings();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.CommandBindings.Clear();
        }

        private void UpdateCommandBindings()
        {
            if (AssociatedObject == null)
                return;

            AssociatedObject.CommandBindings.Clear();

            if (CommandBindings != null)
                AssociatedObject.CommandBindings.AddRange(CommandBindings);

            CommandManager.InvalidateRequerySuggested();
        }
    }
}