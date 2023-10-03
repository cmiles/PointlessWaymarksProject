using System.Text.Json;

namespace PointlessWaymarks.GarminConnect.Converters;

internal static class GarminSerializer
{
    private static readonly JsonSerializerOptions Options;

    static GarminSerializer()
    {
        Options = new JsonSerializerOptions();
        Options.Converters.Add(new NullDoubleConverter());
        Options.Converters.Add(new NullIntConverter());
        Options.Converters.Add(new NullLongConverter());
        Options.Converters.Add(new NullBoolConverter());
    }

    public static TModel To<TModel>(byte[] json)
    {
        return JsonSerializer.Deserialize<TModel>(json, Options);
    }
}