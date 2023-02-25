using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;

namespace PointlessWaymarks.WpfCommon.BoolDataEntry;

public partial class BoolNullableDataEntryContext : ObservableObject, IHasChanges, IHasValidationIssues
{
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private string _helpText = string.Empty;
    [ObservableProperty] private bool? _referenceValue;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private bool? _userValue;
    [ObservableProperty] private List<Func<bool?, IsValid>> _validationFunctions = new();
    [ObservableProperty] private string? _validationMessage = string.Empty;

    private BoolNullableDataEntryContext()
    {
        PropertyChanged += OnPropertyChanged;
    }

    // ReSharper disable once UnusedMember.Global
    public bool UserValueIsNullable => true;

    private void CheckForChangesAndValidate()
    {
        HasChanges = UserValue != ReferenceValue;

        if (ValidationFunctions.Any())
            foreach (var loopValidations in ValidationFunctions)
            {
                var validationResult = loopValidations(UserValue);
                if (!validationResult.Valid)
                {
                    HasValidationIssues = true;
                    ValidationMessage = validationResult.Explanation;
                    return;
                }
            }

        HasValidationIssues = false;
        ValidationMessage = string.Empty;
    }

    public static BoolNullableDataEntryContext CreateInstance()
    {
        return new();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidate();
    }
}