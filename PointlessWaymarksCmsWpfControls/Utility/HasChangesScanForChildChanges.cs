using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Omu.ValueInjecter.Utils;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public static class HasChangesScan
    {
        public static bool ChildPropertiesHaveChanges(object toScan)
        {
            var hasChangesProperties = toScan.GetProps().ToList();

            var hasChanges = false;


            foreach (var loopProperties in hasChangesProperties)
            {
                if (!typeof(IHasChanges).IsAssignableFrom(loopProperties.PropertyType)) continue;

                var value = loopProperties.GetValue(toScan);

                if (value == null) continue;

                hasChanges = hasChanges || ((IHasChanges) value).HasChanges;
            }

            return hasChanges;
        }
    }
}
