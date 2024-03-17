using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.SearchBuilder;

[NotifyPropertyChanged]
public partial class NumericSearchFieldBuilder
{
    public required string FieldTitle { get; set; }
    public bool Not { get; set; }
    public required Func<string, bool> NumberConverterFunction { get; set; }
    public List<string> OperatorChoices { get; set; } = ["==", ">", ">=", "<", "<="];
    public string SelectedOperatorOne { get; set; } = "==";
    public string? SelectedOperatorTwo { get; set; } = "<";
    public string UserNumberTextOne { get; set; } = string.Empty;
    public string UserNumberTextTwo { get; set; } = string.Empty;
}