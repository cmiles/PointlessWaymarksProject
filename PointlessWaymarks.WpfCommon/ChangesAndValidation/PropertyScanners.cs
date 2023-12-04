using System.ComponentModel;

namespace PointlessWaymarks.WpfCommon.ChangesAndValidation;

public static class PropertyScanners
{
    public static bool ChildPropertiesHaveChanges(object toScan)
    {
        return ChildPropertiesHaveChangesWithChangedList(toScan).hasChanges;
    }

    public static (bool hasChanges, List<(bool hasChanges, string description)> changeProperties) ChildPropertiesHaveChangesWithChangedList(object toScan)
    {
        var allProperties = toScan.GetType().GetProperties();

        var hasChanges = false;

        // ReSharper disable once CollectionNeverQueried.Local Left in for debugging
        var propertyList = new List<(bool hasChanges, string propertyInfo)>();

        foreach (var loopProperties in allProperties)
        {
            if (!typeof(IHasChanges).IsAssignableFrom(loopProperties.PropertyType)) continue;

            var value = loopProperties.GetValue(toScan);

            if (value == null) continue;

            var hasChangesValue = ((IHasChanges)value).HasChanges;
            propertyList.Add((hasChangesValue, loopProperties.Name));

            hasChanges = hasChanges || ((IHasChanges)value).HasChanges;
        }

        return (hasChanges, propertyList);
    }

    public static bool ChildPropertiesHaveValidationIssues(object toScan)
    {
        var allProperties = toScan.GetType().GetProperties();

        var hasValidationIssues = false;

        // ReSharper disable once CollectionNeverQueried.Local Left in for debugging
        var propertyList = new List<(bool hasChanges, string propertyInfo)>();

        foreach (var loopProperties in allProperties)
        {
            if (!typeof(IHasValidationIssues).IsAssignableFrom(loopProperties.PropertyType)) continue;

            var value = loopProperties.GetValue(toScan);

            if (value == null) continue;

            var hasValidationValue = ((IHasValidationIssues)value).HasValidationIssues;
            propertyList.Add((hasValidationValue, loopProperties.Name));

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
                    if (string.IsNullOrWhiteSpace(args.PropertyName)) return;
                    if (args.PropertyName is "HasChanges" or "HasValidationIssues")
                        actionOnStatusChange();
                };
            }

        actionOnStatusChange.Invoke();
    }
}