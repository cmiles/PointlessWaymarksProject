using System.ComponentModel;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.SearchBuilder;

[NotifyPropertyChanged]
public partial class BoundsSearchFieldBuilder
{
    public BoundsSearchFieldBuilder()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public required string FieldTitle { get; set; }
    public bool Not { get; set; }
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

        if (!decimal.TryParse(toConvert, out var result)) return false;

        return result is < -90 or > 90;
    }

    private bool CanConvertLongitude(string? toConvert)
    {
        if (string.IsNullOrWhiteSpace(toConvert)) return false;

        if (!decimal.TryParse(toConvert, out var result)) return false;

        return result is > 180 or < -180;
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