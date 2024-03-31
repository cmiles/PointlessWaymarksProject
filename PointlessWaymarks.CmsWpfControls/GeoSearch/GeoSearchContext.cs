using System.Collections.ObjectModel;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools.GeoNames;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.GeoSearch;

[NotifyPropertyChanged]
[GenerateStatusCommands]
[StaThreadConstructorGuard]
public partial class GeoSearchContext
{
    public GeoSearchContext(StatusControlContext statusContext)
    {
        SearchResults = new ObservableCollection<GeoNamesSimpleSearchResult>();
        StatusContext = statusContext;
        GeoNamesUserName = GeoNamesApiCredentials.GetGeoNamesSiteCredentials();

        ApiAvailable = !string.IsNullOrWhiteSpace(GeoNamesUserName);

        BuildCommands();

        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(UserSearchString)) StatusContext.RunNonBlockingTask(RunSearch);
        };
    }

    public bool ApiAvailable { get; set; }
    public string GeoNamesUserName { get; set; }
    public ObservableCollection<GeoNamesSimpleSearchResult> SearchResults { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string UserSearchString { get; set; } = string.Empty;

    public static async Task<GeoSearchContext> CreateInstance(StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var newContext = new GeoSearchContext(statusContext);
        return newContext;
    }

    public event EventHandler<LocationSelectedEventArgs>? LocationSelected;

    protected virtual void OnLocationSelected(double latitude, double longitude)
    {
        LocationSelected?.Invoke(this, new LocationSelectedEventArgs(latitude, longitude));
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
        var searchResults = await GeoNamesSearch.SearchSimple(UserSearchString, GeoNamesUserName);

        await ThreadSwitcher.ResumeForegroundAsync();
        SearchResults.Clear();
        searchResults.ForEach(x => SearchResults.Add(x));
    }

    [NonBlockingCommand]
    public async Task SelectLocation(GeoNamesSimpleSearchResult selected)
    {
        OnLocationSelected(selected.Latitude, selected.Longitude);
    }

    public class LocationSelectedEventArgs(double latitude, double longitude) : EventArgs
    {
        public double Latitude { get; set; } = latitude;
        public double Longitude { get; set; } = longitude;
    }
}