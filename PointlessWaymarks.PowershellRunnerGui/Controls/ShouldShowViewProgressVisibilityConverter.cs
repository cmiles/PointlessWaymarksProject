using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls
{
    public sealed class ScriptJobRunGuiViewShowProgressViewConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value == null) return null;
            if (value is not ScriptJobRunGuiView guiView) return Visibility.Collapsed;
            if(guiView.CompletedOnUtc.HasValue) return Visibility.Collapsed;
            if(guiView.PersistentId.ToString("N").StartsWith("000000000000")) return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
