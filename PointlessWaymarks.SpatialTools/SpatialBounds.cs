using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointlessWaymarks.SpatialTools
{
    public record SpatialBounds(double MaxLatitude, double MaxLongitude,
        double MinLatitude, double MinLongitude);
}
