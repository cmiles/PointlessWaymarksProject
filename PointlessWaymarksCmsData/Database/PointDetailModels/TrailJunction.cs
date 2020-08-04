using System;
using System.Collections.Generic;
using System.Text;

namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class TrailJunction
    {
        public const string DataTypeIdentifier = "TrailJunction";

        public class TrailJunction01
        {
            public string Notes { get; set; }
            public string Version { get; } = "1";
        }

    }
}
