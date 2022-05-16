﻿using System.Collections.ObjectModel;

namespace PointlessWaymarks.CmsData;

public static class ListHelpers
{
    public static List<T> AddIfNotNull<T>(this List<T> thisList, T? toAdd)
    {
        if (toAdd is null) return thisList;
        thisList.Add(toAdd);
        return thisList;
    }

    public static List<T> AsList<T>(this T item)
    {
        return new() { item };
    }

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

    /// <summary>
    /// Like 'Any' but takes and Func<T, Task<bool>> and applies it in a For Each loop.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public static async Task<bool> AnyAsyncLoop<T>(
        this IEnumerable<T> source, Func<T, Task<bool>> func)
    {
        foreach (var element in source)
        {
            if (await func(element))
                return true;
        }

        return false;
    }
}