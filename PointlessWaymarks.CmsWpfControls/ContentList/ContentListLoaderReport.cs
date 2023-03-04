using PointlessWaymarks.CmsWpfControls.ColumnSort;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

public class ContentListLoaderReport : ContentListLoaderBase, IContentListLoader
{
    private readonly ColumnSortControlContext? _columnSort;
    private readonly Func<Task<List<object>>>? _loaderFunc;

    public ContentListLoaderReport(Func<Task<List<object>>> loaderFunc,
        ColumnSortControlContext? sorting = null) :
        base("Report Results", null)
    {
        _loaderFunc = loaderFunc;
        _columnSort = sorting;
        AddNewItemsFromDataNotifications = false;
    }

    public override async Task<List<object>> LoadItems(IProgress<string>? progress = null)
    {
        var listItems = new List<object>();

        if (_loaderFunc != null) listItems.AddRange(await _loaderFunc());

        AllItemsLoaded = true;

        return listItems;
    }

    public ColumnSortControlContext SortContext()
    {
        return _columnSort ?? SortContextDefault();
    }
}