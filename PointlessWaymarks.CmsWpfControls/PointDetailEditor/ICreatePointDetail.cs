using System.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.PointDetailEditor;

public interface IPointDetailEditor : INotifyPropertyChanged
{
    PointDetail DbEntry { get; }
    public bool HasChanges { get; }
    public bool HasValidationIssues { get; set; }
    PointDetail CurrentPointDetail();
}