namespace PointlessWaymarks.CommonTools;

public static class ListTools
{
    public static List<T> AddIfNotNull<T>(this List<T> thisList, T? toAdd)
    {
        if (toAdd is null) return thisList;
        thisList.Add(toAdd);
        return thisList;
    }

    /// <summary>
    ///     Like 'Any' but takes and Func T, Task bool and applies it in a For Each loop.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public static async Task<bool> AnyAsyncLoop<T>(
        this IEnumerable<T> source, Func<T, Task<bool>> func)
    {
        foreach (var element in source)
            if (await func(element))
                return true;

        return false;
    }

    public static List<T> AsList<T>(this T item)
    {
        return [item];
    }

    /// <summary>
    /// Async Select one at a time.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="asyncSelector"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static async Task<List<TResult>> SelectInSequenceAsync<TSource, TResult>(this IEnumerable<TSource> source,
        Func<TSource, Task<TResult>> asyncSelector)
    {
        var result = new List<TResult>();
        foreach (var s in source) result.Add(await asyncSelector(s));

        return result;
    }
}