namespace PointlessWaymarks.CommonTools;

public static class UnitConversionTools
{
    public static double FeetToMeters(this double feet)
    {
        return feet * 0.3048;
    }

    public static double FeetToMeters(this double? feet)
    {
        if (feet == null) return 0;
        return feet.Value.FeetToMeters();
    }

    public static double MetersToFeet(this double meters)
    {
        return meters / 0.3048;
    }

    public static double MetersToFeet(this double? meters)
    {
        if (meters == null) return 0;
        return meters.Value.MetersToFeet();
    }

    public static double MetersToMiles(this double meters)
    {
        return meters / 1609.344;
    }

    public static double MetersToMiles(this double? meters)
    {
        if (meters == null) return 0;
        return meters.Value.MetersToFeet();
    }

    public static double MilesToMeters(this double miles)
    {
        return miles * 1609.344;
    }

    public static double MilesToMeters(this double? miles)
    {
        if (miles == null) return 0;
        return miles.Value.FeetToMeters();
    }
}