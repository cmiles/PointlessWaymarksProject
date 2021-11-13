using System.ComponentModel.DataAnnotations;

namespace PointlessWaymarks.CmsData.Database.Models
{
    public class GenerationChangedContentId
    {
        [Key] public Guid ContentId { get; set; }
    }
}