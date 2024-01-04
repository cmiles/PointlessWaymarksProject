using System.Collections;

namespace PointlessWaymarks.WpfCommon.Behaviors;

/// <summary>
///     This Comparer tries to find numeric values and sort them and falls back to string sorting as
///     is has to. This behavior is designed to help with sorting with a WPF Datagrid where especially for ease
///     of use (but also for mixed values and other scenarios) you have used a DataGridTextColumn and
///     by default are getting string rather than numeric sorting.
/// </summary>
public class SimpleNumberFirstToStringCompare : IComparer
{
    public int Compare(object? x, object? y)
    {
        if (x is null && y is null) return 0;
        if (x is not null && y is null) return 1;
        if (x is null && y is not null) return -1;

        var xIsNumber = double.TryParse(x!.ToString(), out var xd);

        var yIsNumber = double.TryParse(y!.ToString(), out var yd);

        if (xIsNumber && yIsNumber) return xd.CompareTo(yd);

        var xs = x.ToString() ?? string.Empty;
        var ys = y.ToString() ?? string.Empty;

        return string.Compare(xs, ys, StringComparison.OrdinalIgnoreCase);
    }
}