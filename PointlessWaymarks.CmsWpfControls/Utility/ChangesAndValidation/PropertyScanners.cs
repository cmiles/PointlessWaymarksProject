using System;
using System.ComponentModel;
using System.Linq;
using Omu.ValueInjecter.Utils;

namespace PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation
{
    public static class PropertyScanners
    {
        public static bool ChildPropertiesHaveChanges(object toScan)
        {
            var allProperties = toScan.GetProps().ToList();

            var hasChanges = false;

            foreach (var loopProperties in allProperties)
            {
                if (!typeof(IHasChanges).IsAssignableFrom(loopProperties.PropertyType)) continue;

                var value = loopProperties.GetValue(toScan);

                if (value == null) continue;

                hasChanges = hasChanges || ((IHasChanges) value).HasChanges;
            }

            return hasChanges;
        }

        public static bool ChildPropertiesHaveValidationIssues(object toScan)
        {
            var allProperties = toScan.GetProps().ToList();

            var hasValidationIssues = false;

            foreach (var loopProperties in allProperties)
            {
                if (!typeof(IHasValidationIssues).IsAssignableFrom(loopProperties.PropertyType)) continue;

                var value = loopProperties.GetValue(toScan);

                if (value == null) continue;

                hasValidationIssues = hasValidationIssues || ((IHasValidationIssues) value).HasValidationIssues;
            }

            return hasValidationIssues;
        }

        public static void SubscribeToChildHasChangesAndHasValidationIssues(object toScan, Action actionOnStatusChange)
        {
            var allProperties = toScan.GetProps().ToList();

            foreach (var loopProperties in allProperties)
                if (typeof(IHasChanges).IsAssignableFrom(loopProperties.PropertyType) &&
                    typeof(INotifyPropertyChanged).IsAssignableFrom(loopProperties.PropertyType))
                {
                    var value = loopProperties.GetValue(toScan);
                    if (value == null) continue;

                    ((INotifyPropertyChanged) value).PropertyChanged += (sender, args) =>
                    {
                        if (string.IsNullOrWhiteSpace(args?.PropertyName) || actionOnStatusChange == null) return;
                        if (args.PropertyName == "HasChanges" || args.PropertyName == "HasValidationIssues")
                            actionOnStatusChange();
                    };
                }

            actionOnStatusChange.Invoke();
        }
    }
}