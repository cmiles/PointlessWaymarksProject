using System;
using System.Globalization;

namespace PointlessWaymarksCmsData
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class BuildDateAttribute : Attribute
    {
        public BuildDateAttribute(string value)
        {
            DateTime = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        public DateTime DateTime { get; }
    }
}