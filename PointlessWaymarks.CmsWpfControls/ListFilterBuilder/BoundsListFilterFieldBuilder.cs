using System.ComponentModel;
using System.Globalization;
using System.Windows;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.ListFilterBuilder;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class BoundsListFilterFieldBuilder
{
    public BoundsListFilterFieldBuilder(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        BuildCommands();
        PropertyChanged += OnPropertyChanged;
    }

    public SpatialBounds? CurrentBounds => AllConvert()
        ? new SpatialBounds(double.Parse(UserMaxLatitude!), double.Parse(UserMaxLongitude!),
            double.Parse(UserMinLatitude!),
            double.Parse(UserMinLongitude!))
        : null;

    public required string FieldTitle { get; set; }
    public bool Not { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string? UserMaxLatitude { get; set; }
    public bool UserMaxLatitudeConverts { get; set; }
    public string? UserMaxLongitude { get; set; }
    public bool UserMaxLongitudeConverts { get; set; }
    public string? UserMinLatitude { get; set; }
    public bool UserMinLatitudeConverts { get; set; }
    public string? UserMinLongitude { get; set; }
    public bool UserMinLongitudeConverts { get; set; }

    public bool AllConvert()
    {
        return UserMinLatitudeConverts && UserMaxLatitudeConverts && UserMinLongitudeConverts &&
               UserMaxLongitudeConverts;
    }

    private bool CanConvertLatitude(string? toConvert)
    {
        if (string.IsNullOrWhiteSpace(toConvert)) return false;

        if (!double.TryParse(toConvert, out var result)) return false;

        return result is >= -90 and <= 90;
    }

    private bool CanConvertLongitude(string? toConvert)
    {
        if (string.IsNullOrWhiteSpace(toConvert)) return false;

        if (!double.TryParse(toConvert, out var result)) return false;

        return result is >= -180 and <= 180;
    }

    [NonBlockingCommand]
    public async Task GetBoundsFromMap()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var modal = await LocationBoundsChooserWindow.CreateInstance(CurrentBounds, "Search Builder");
        modal.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive) ??
                      Application.Current.Windows.OfType<Window>().FirstOrDefault();
        var result = modal.ShowDialog();
        if (result ?? false)
        {
            UserMinLatitude = modal.LocationChooser?.MapBounds?.MinLatitude.ToString("F6", CultureInfo.InvariantCulture) ??
                              string.Empty;
            UserMaxLatitude = modal.LocationChooser?.MapBounds?.MaxLatitude.ToString("F6", CultureInfo.InvariantCulture) ??
                              string.Empty;
            UserMinLongitude = modal.LocationChooser?.MapBounds?.MinLongitude.ToString("F6", CultureInfo.InvariantCulture) ??
                               string.Empty;
            UserMaxLongitude = modal.LocationChooser?.MapBounds?.MaxLongitude.ToString("F6", CultureInfo.InvariantCulture) ??
                               string.Empty;
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(UserMinLatitude):
                UserMinLatitudeConverts = CanConvertLatitude(UserMinLatitude);
                break;
            case nameof(UserMaxLatitude):
                UserMaxLatitudeConverts = CanConvertLatitude(UserMaxLatitude);
                break;
            case nameof(UserMinLongitude):
                UserMinLongitudeConverts = CanConvertLongitude(UserMinLongitude);
                break;
            case nameof(UserMaxLongitude):
                UserMaxLongitudeConverts = CanConvertLongitude(UserMaxLongitude);
                break;
        }
    }
}