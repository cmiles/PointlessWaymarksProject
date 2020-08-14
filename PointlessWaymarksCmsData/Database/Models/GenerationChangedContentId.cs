using System;
using System.ComponentModel.DataAnnotations;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class GenerationChangedContentId
    {
        [Key] public Guid ContentId { get; set; }
    }
}