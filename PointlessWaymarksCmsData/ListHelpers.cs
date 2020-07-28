using System;
using System.Collections.Generic;
using System.Linq;

namespace PointlessWaymarksCmsData
{
    public static class ListHelpers
    {
        public static List<T> AddIfNotNull<T>(this List<T> thisList, T toAdd)
        {
            if (toAdd is null) return thisList;
            thisList.Add(toAdd);
            return thisList;
        }

        public static List<T> AsList<T>(this T item)
        {
            return new List<T> {item};
        }

        public static IEnumerable<List<T>> Partition<T>(this IList<T> source, int size)
        {
            for (var i = 0; i < Math.Ceiling(source.Count / (double) size); i++)
                yield return new List<T>(source.Skip(size * i).Take(size));
        }
    }
}