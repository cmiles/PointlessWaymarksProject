using System.ComponentModel;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsWpfControls.PointDetailEditor
{
    public interface IPointDetailEditor : INotifyPropertyChanged
    {
        PointDetail DbEntry { get; }
        public bool HasChanges { get; }
        public bool HasValidationIssues { get; set; }
        PointDetail CurrentPointDetail();
    }
}