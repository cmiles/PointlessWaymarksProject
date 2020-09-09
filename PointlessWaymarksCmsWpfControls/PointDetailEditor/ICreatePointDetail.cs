using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsWpfControls.PointDetailEditor
{
    public interface IPointDetailEditor
    {
        PointDetail DbEntry { get; }
        PointDetail CurrentPointDetail();
    }
}