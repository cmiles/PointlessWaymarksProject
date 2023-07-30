using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Xaml.Behaviors;

namespace PointlessWaymarks.WpfCommon.Behaviors;

/// <summary>
/// The Delay value of the TextBox Binding can not be bound directly - this behavior
/// provides a way to indirectly bind the value. BEWARE not every binding option
/// is supported!!! Currently Path and Update Source Trigger
/// will be used from the original/XAML binding, other binding properties WILL NOT
/// be preserved by this behavior if the Delay is changed (notably Source and Converter
/// will be lost).
/// </summary>
public class TextBoxDelayBindingBehavior : Behavior<TextBox>
{
    //Inspiration from https://stackoverflow.com/questions/43763831/binding-the-delay-property-of-a-binding
    public static readonly DependencyProperty DelayBindingProperty =
        DependencyProperty.Register(nameof(DelayBinding), typeof(int?),
            typeof(TextBoxDelayBindingBehavior), new PropertyMetadata(null, OnDelayBindingDelayPropertyChanged));

    public int? DelayBinding
    {
        get => (int?)GetValue(DelayBindingProperty);
        set => SetValue(DelayBindingProperty, value);
    }

    private static void OnDelayBindingDelayPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var source = d as TextBoxDelayBindingBehavior;
        var attached = source?.AssociatedObject;
        if (attached is null) return;

        var currentBinding = BindingOperations.GetBinding(attached, TextBox.TextProperty);
        if (currentBinding is null) return;

        if (e.NewValue is not int newDelay) return;

        if (currentBinding.Delay == newDelay) return;

        BindingOperations.ClearBinding(attached, TextBox.TextProperty);

        var updatedBinding = new Binding
        {
            Path = currentBinding.Path,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Delay = newDelay
        };

        BindingOperations.SetBinding(attached, TextBox.TextProperty, updatedBinding);
    }
}