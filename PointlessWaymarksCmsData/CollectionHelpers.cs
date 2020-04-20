using System.Collections.Generic;

namespace PointlessWaymarksCmsData
{
    public static class CollectionHelpers
    {
        public static List<T> AddIfNotNull<T>(this List<T> thisList, T toAdd)
        {
            if (toAdd is null) return thisList;
            thisList.Add(toAdd);
            return thisList;
        }
    }
}