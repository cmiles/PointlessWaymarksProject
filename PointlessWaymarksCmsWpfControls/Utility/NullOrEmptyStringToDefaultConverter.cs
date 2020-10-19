using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using FluentMigrator.Runner.Initialization;
using PointlessWaymarksCmsData;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public class NullOrWhiteSpaceStringToDefaultConverter : IValueConverter
    {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (string.IsNullOrWhiteSpace(value?.ToString())) return parameter;

                return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }

    }
}
