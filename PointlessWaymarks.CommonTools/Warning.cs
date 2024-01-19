namespace PointlessWaymarks.CommonTools;

public struct Warning
{
}

public struct Warning<T>(T value)
{
    public T Value { get; } = value;
}