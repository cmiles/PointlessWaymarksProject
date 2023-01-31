using System.ComponentModel;

namespace PointlessWaymarks.WpfCommon.ChangesAndValidation;

public static class PropertyScanners
{
    public static bool ChildPropertiesHaveChanges(object toScan)
    {
        var allProperties = toScan.GetType().GetProperties();

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
        var allProperties = toScan.GetType().GetProperties();

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
        var allProperties = toScan.GetType().GetProperties();

        foreach (var loopProperties in allProperties)
            if (typeof(IHasChanges).IsAssignableFrom(loopProperties.PropertyType) &&
                typeof(INotifyPropertyChanged).IsAssignableFrom(loopProperties.PropertyType))
            {
                var value = loopProperties.GetValue(toScan);
                if (value == null) continue;

                ((INotifyPropertyChanged) value).PropertyChanged += (_, args) =>
                {
                    if (string.IsNullOrWhiteSpace(args?.PropertyName) || actionOnStatusChange == null) return;
                    if (args.PropertyName is "HasChanges" or "HasValidationIssues")
                        actionOnStatusChange();
                };
            }

        actionOnStatusChange.Invoke();
    }
}