using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            //https://stackoverflow.com/questions/19112922/sort-observablecollectionstring-through-c-sharp
            var sortableList = new List<T>(collection);
            sortableList.Sort(comparison);

            for (var i = 0; i < sortableList.Count; i++) collection.Move(collection.IndexOf(sortableList[i]), i);
        }

        public static void SortBy<TSource, TKey>(this ObservableCollection<TSource> collection, Func<TSource, TKey> keySelector)
        {
            List<TSource> sorted = collection.OrderBy(keySelector).ToList();
            for (int i = 0; i < sorted.Count; i++)
                collection.Move(collection.IndexOf(sorted[i]), i);
        }

        public static void SortByDescending<TSource, TKey>(this ObservableCollection<TSource> collection, Func<TSource, TKey> keySelector)
        {
            List<TSource> sorted = collection.OrderByDescending(keySelector).ToList();
            for (int i = 0; i < sorted.Count; i++)
                collection.Move(collection.IndexOf(sorted[i]), i);
        }
    }
}