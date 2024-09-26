using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using ICSharpCode.AvalonEdit;
using Microsoft.Xaml.Behaviors;

namespace PointlessWaymarks.PowerShellRunnerGui.PowerShellEditor;

public sealed class AvalonEditRenderedStringBindingBehaviour : Behavior<TextEditor>
{
    private string _textFromTextEditor
        ;

    //[c# - Two Way Binding to AvalonEdit Document Text using MVVM - Stack Overflow](https://stackoverflow.com/questions/18964176/two-way-binding-to-avalonedit-document-text-using-mvvm)
    public static readonly DependencyProperty RenderedTextProperty =
        DependencyProperty.Register(nameof(RenderedText), typeof(string),
            typeof(AvalonEditRenderedStringBindingBehaviour),
            new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                PropertyChangedCallback));

    public string RenderedText
    {
        get => (string)GetValue(RenderedTextProperty);
        set => SetValue(RenderedTextProperty, value);
    }

    private void AssociatedObjectOnTextChanged(object? sender, EventArgs eventArgs)
    {
        if (sender is TextEditor { Document: not null } textEditor)
        {
            _textFromTextEditor = textEditor.Document.Text;
            RenderedText = _textFromTextEditor;
        }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.TextChanged += AssociatedObjectOnTextChanged;
            AssociatedObject.Text = RenderedText;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
            AssociatedObject.TextChanged -= AssociatedObjectOnTextChanged;
    }

    private static void PropertyChangedCallback(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
    {
        var behavior = dependencyObject as AvalonEditRenderedStringBindingBehaviour;
        var editor = behavior?.AssociatedObject;
        if (editor is null) return;

        var newValue = dependencyPropertyChangedEventArgs.NewValue.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(newValue) && string.IsNullOrWhiteSpace(behavior!._textFromTextEditor)) return;

        if (newValue.Equals(behavior!._textFromTextEditor)) return;

        if (editor.Document != null)
        {
            var caretOffset = editor.CaretOffset;
            editor.Document.Text = dependencyPropertyChangedEventArgs.NewValue != null
                ? dependencyPropertyChangedEventArgs.NewValue.ToString()
                : string.Empty;
            editor.CaretOffset = editor.Document.TextLength < caretOffset ? editor.Document.TextLength : caretOffset;
        }
    }
}