using System.ComponentModel;
using PointlessWaymarks.CmsWpfControls.DropdownDataEntry;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;

namespace PointlessWaymarks.WpfCommon.StringDropdownDataEntry;

[NotifyPropertyChanged]
public partial class StringDropdownDataEntryContext : IHasChanges, IHasValidationIssues
{
    private StringDropdownDataEntryContext()
    {
        PropertyChanged += OnPropertyChanged;
    }
    
    public List<DropDownDataChoice> Choices { get; set; } = [];
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    public string HelpText { get; set; } = string.Empty;
    public string? ReferenceValue { get; set; }
    public string Title { get; set; } = string.Empty;
    public DropDownDataChoice? SelectedItem { get; set; }
    public string UserValue => SelectedItem?.DataString ?? string.Empty;
    public List<Func<string?, IsValid>> ValidationFunctions { get; set; } = [];
    public string ValidationMessage { get; set; } = string.Empty;
    
    private void CheckForChangesAndValidate()
    {
        HasChanges = UserValue.TrimNullToEmpty() != ReferenceValue.TrimNullToEmpty();
        
        if (ValidationFunctions.Any())
            foreach (var loopValidations in ValidationFunctions)
            {
                var (passed, validationMessage) = loopValidations(UserValue);
                if (!passed)
                {
                    HasValidationIssues = true;
                    ValidationMessage = validationMessage;
                    return;
                }
            }
        
        HasValidationIssues = false;
        ValidationMessage = string.Empty;
    }
    
    public static StringDropdownDataEntryContext CreateInstance()
    {
        return new StringDropdownDataEntryContext();
    }
    
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        
        if (e.PropertyName.Equals(nameof(HasChanges)) || e.PropertyName.Equals(nameof(HasValidationIssues)) ||
            e.PropertyName.Equals(nameof(ValidationMessage))) return;
        
        CheckForChangesAndValidate();

        if (e.PropertyName.Equals(nameof(Choices)))
        {
            if (!Choices.Any())
            {
                SelectedItem = null;
                return;
            }
            
            if (!Choices.Any(x => x.DataString.Equals(UserValue))) SelectedItem = Choices.First();
        }
    }
    
    public void TrySetUserValue(string userValue)
    {
        if (!Choices.Any()) SelectedItem = null;
        if (Choices.Any(x => x.DataString.Equals(userValue))) SelectedItem = Choices.First(x => x.DataString.Equals(userValue));
        return;
    }
}