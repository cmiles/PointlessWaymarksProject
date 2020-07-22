using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class RelatedContent
    {
        public Guid ContentOne { get; set; }
        public Guid ContentTwo { get; set; }
        public int Id { get; set; }
    }
}