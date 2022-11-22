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
        return new List<T> { item };
    }
}