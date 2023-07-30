namespace PointlessWaymarks.CommonTools;

public struct Warning
{
}

public struct Warning<T>
{
    public Warning(T value)
    {
        Value = value;
    }

    public T Value { get; }
}