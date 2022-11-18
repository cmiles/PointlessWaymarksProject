using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.FeatureIntersectionTaggingGui.Models
{
    [ObservableObject]
    public partial class IntersectFileViewModel
    {
        [ObservableProperty] private string _source = string.Empty;
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private List<string> _attributesForTags = new();
        [ObservableProperty] private string _tagAll = string.Empty;
        [ObservableProperty] private string _fileName = string.Empty;
    }
}
