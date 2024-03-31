using System.Collections.ObjectModel;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools.GeoNames;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.GeoSearch;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class GeoSearchContext
{
    public GeoSearchContext(StatusControlContext statusContext)
    {
        SearchResults = new ObservableCollection<GeoNamesSimpleSearchEntry>();
        StatusContext = statusContext;
        GeoNamesUserName = GeoNamesApiCredentials.GetGeoNamesSiteCredentials();

        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(UserSearchString)) StatusContext.RunNonBlockingTask(RunSearch);
        };
    }

    public string GeoNamesUserName { get; set; }
    public ObservableCollection<GeoNamesSimpleSearchEntry> SearchResults { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string UserSearchString { get; set; } = string.Empty;

    public static async Task<GeoSearchContext> CreateInstance(StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var newContext = new GeoSearchContext(statusContext);
        return newContext;
    }

    public async Task RunSearch()
    {
        if (string.IsNullOrWhiteSpace(UserSearchString))
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            SearchResults.Clear();
            return;
        }

        await ThreadSwitcher.ResumeBackgroundAsync();
        var searchResults = await GeoNamesSearch.SearchSimple(UserSearchString, GeoNamesUserName, "NA", "US");

        await ThreadSwitcher.ResumeForegroundAsync();
        SearchResults.Clear();
        searchResults.ForEach(x => SearchResults.Add(x));
    }
}