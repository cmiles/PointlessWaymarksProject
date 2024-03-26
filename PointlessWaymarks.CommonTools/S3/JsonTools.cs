using System.Text.Json;

namespace PointlessWaymarks.CommonTools.S3;

public class JsonTools
{
    public static JsonSerializerOptions WriteIndentedOptions = new() { WriteIndented = true };
}