using System.ComponentModel;
using System.IO;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.GeoToolsGui.Models;

[NotifyPropertyChanged]
public partial class FeatureFileContext
{
    public FeatureFileContext()
    {
        Validate();
        PropertyChanged += OnPropertyChanged;
    }

    public List<string> AttributesForTags { get; set; } = new();
    public Guid ContentId { get; set; } = Guid.NewGuid();
    public string Downloaded { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool HasWarnings { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string TagAll { get; set; } = string.Empty;
    public List<string> Warnings { get; } = new();

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Validate();
    }

    private void Validate()
    {
        if (string.IsNullOrEmpty(TagAll) && !AttributesForTags.Any())
        {
            HasWarnings = true;
            Warnings.Add("Without a value for Tag All or Attributes for Tags no tags will be produced.");
        }

        if (string.IsNullOrEmpty(FileName))
        {
            HasWarnings = true;
            Warnings.Add("Filename is blank?");
        }

        if (!string.IsNullOrWhiteSpace(FileName) && !File.Exists(FileName))
        {
            HasWarnings = true;
            Warnings.Add("File doesn't exist?");
        }
    }
}