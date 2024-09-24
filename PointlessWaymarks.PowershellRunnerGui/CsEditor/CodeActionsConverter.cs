using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Markup;
using Metalama.Framework.Code.Collections;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using RoslynPad.Roslyn.CodeActions;

namespace PointlessWaymarks.PowerShellRunnerGui.CsEditor;

internal sealed class CodeActionsConverter : MarkupExtension, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as CodeAction).GetCodeActions();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
