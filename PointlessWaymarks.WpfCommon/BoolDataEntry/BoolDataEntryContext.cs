using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;

namespace PointlessWaymarks.WpfCommon.BoolDataEntry;

public partial class BoolDataEntryContext : ObservableObject, IHasChanges, IHasValidationIssues
{
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private string _helpText;
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private bool _referenceValue;
    [ObservableProperty] private string _title;
    [ObservableProperty] private bool _userValue;
    [ObservableProperty] private List<Func<bool, IsValid>> _validationFunctions = new();
    [ObservableProperty] private string _validationMessage;

    private BoolDataEntryContext()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public bool UserValueIsNullable => false;

    public void CheckForChangesAndValidate()
    {
        HasChanges = UserValue != ReferenceValue;

        if (ValidationFunctions != null && Enumerable.Any<Func<bool, IsValid>>(ValidationFunctions))
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

    public static BoolDataEntryContext CreateInstance()
    {
        return new BoolDataEntryContext();
    }

    
    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidate();
    }
}