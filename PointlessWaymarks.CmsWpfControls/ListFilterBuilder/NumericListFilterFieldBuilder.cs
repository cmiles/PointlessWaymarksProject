using System.ComponentModel;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.ListFilterBuilder;

[NotifyPropertyChanged]
public partial class NumericListFilterFieldBuilder
{
    public NumericListFilterFieldBuilder()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public bool EnableNumberTwo { get; set; }
    public required string FieldTitle { get; set; }
    public bool Not { get; set; }
    public required Func<string, bool> NumberConverterFunction { get; set; }
    public List<string> OperatorChoices { get; set; } = ["==", ">", ">=", "<", "<="];
    public string SelectedOperatorOne { get; set; } = "==";
    public string? SelectedOperatorTwo { get; set; } = "<";
    public bool ShowNumberOneTextWarning { get; set; }
    public bool ShowNumberTwoTextWarning { get; set; }
    public bool UserNumberOneTextConverts { get; set; }
    public string UserNumberTextOne { get; set; } = string.Empty;
    public string UserNumberTextTwo { get; set; } = string.Empty;
    public bool UserNumberTwoTextConverts { get; set; }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName?.Equals(nameof(UserNumberTextOne)) ?? false)
        {
            UserNumberOneTextConverts = NumberConverterFunction(UserNumberTextOne);
            ShowNumberOneTextWarning = !string.IsNullOrWhiteSpace(UserNumberTextOne) && !UserNumberOneTextConverts;
        }

        if ((e.PropertyName?.Equals(nameof(UserNumberTextOne)) ?? false) ||
            (e.PropertyName?.Equals(nameof(SelectedOperatorOne)) ?? false))
            EnableNumberTwo = UserNumberOneTextConverts && SelectedOperatorOne != "==";

        if (e.PropertyName?.Equals(nameof(UserNumberTextTwo)) ?? false)
        {
            UserNumberTwoTextConverts = NumberConverterFunction(UserNumberTextTwo);
            ShowNumberTwoTextWarning = EnableNumberTwo && (!UserNumberOneTextConverts ||
                                                           (!string.IsNullOrWhiteSpace(UserNumberTextTwo) &&
                                                            !UserNumberTwoTextConverts));
        }
    }
}