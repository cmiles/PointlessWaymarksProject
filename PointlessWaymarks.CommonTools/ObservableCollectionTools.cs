using System.Collections.ObjectModel;

namespace PointlessWaymarks.CommonTools;

public static class ObservableCollectionTools
{
    public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
    {
        //https://stackoverflow.com/questions/19112922/sort-observablecollectionstring-through-c-sharp
        var sortableList = new List<T>(collection);
        sortableList.Sort(comparison);

        for (var i = 0; i < sortableList.Count; i++) collection.Move(collection.IndexOf(sortableList[i]), i);
    }

    public static void SortBy<TSource, TKey>(this ObservableCollection<TSource> collection,
        Func<TSource, TKey> keySelector)
    {
        var sorted = collection.OrderBy(keySelector).ToList();
        for (var i = 0; i < sorted.Count; i++)
            collection.Move(collection.IndexOf(sorted[i]), i);
    }

    public static void SortByDescending<TSource, TKey>(this ObservableCollection<TSource> collection,
        Func<TSource, TKey> keySelector)
    {
        var sorted = collection.OrderByDescending(keySelector).ToList();
        for (var i = 0; i < sorted.Count; i++)
            collection.Move(collection.IndexOf(sorted[i]), i);
    }
}