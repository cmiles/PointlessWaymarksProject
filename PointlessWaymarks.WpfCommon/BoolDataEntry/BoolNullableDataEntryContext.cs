using System.ComponentModel;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;

namespace PointlessWaymarks.WpfCommon.BoolDataEntry;

[NotifyPropertyChanged]
public partial class BoolNullableDataEntryContext : IHasChanges, IHasValidationIssues
{
    private BoolNullableDataEntryContext()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public string HelpText { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool? ReferenceValue { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool? UserValue { get; set; }

    // ReSharper disable once UnusedMember.Global
    public bool UserValueIsNullable => true;

    public List<Func<bool?, IsValid>> ValidationFunctions { get; set; } =
        [];

    public string ValidationMessage { get; set; } = string.Empty;
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

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
        return new BoolNullableDataEntryContext();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        
        if (e.PropertyName.Equals(nameof(HasChanges)) || e.PropertyName.Equals(nameof(HasValidationIssues)) ||
            e.PropertyName.Equals(nameof(ValidationMessage))) return;
        
        CheckForChangesAndValidate();
    }
}