using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class RelatedContent
    {
        public int Id { get; set; }
        public Guid ContentOne { get; set; }
        public int ContentTwo { get; set; }
    }
}