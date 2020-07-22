using System;
using System.ComponentModel.DataAnnotations;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class GenerationContentIdReference
    {
        [Key] public Guid ContentId { get; set; }
    }
}