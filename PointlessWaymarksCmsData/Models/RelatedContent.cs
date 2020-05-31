using System;
using System.Collections.Generic;
using System.Text;

namespace PointlessWaymarksCmsData.Models
{
    public class RelatedContent
    {
        public int Id { get; set; }
        public Guid ContentOne { get; set; }
        public int ContentTwo { get; set; }
    }
}
