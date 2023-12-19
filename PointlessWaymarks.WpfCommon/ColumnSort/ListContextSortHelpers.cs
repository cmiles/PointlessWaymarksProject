using System.ComponentModel;
using System.Windows.Data;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using TypeSupport.Extensions;

namespace PointlessWaymarks.WpfCommon.ColumnSort;

public static class ListContextSortHelpers
{
    public static async Task SortList(List<SortDescription>? listSorts, object? items)
    {
        if (items == null) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        if (CollectionViewSource.GetDefaultView(items) is ListCollectionView listCollectionView)
        {
            listCollectionView.IsLiveSorting = true;
        }

        var collectionView = CollectionViewSource.GetDefaultView(items) as CollectionView;

        if (collectionView == null) return;

        collectionView.SortDescriptions.Clear();

        if (listSorts == null || listSorts.Count < 1) return;
        foreach (var loopSorts in listSorts) collectionView.SortDescriptions.Add(loopSorts);
    }
}