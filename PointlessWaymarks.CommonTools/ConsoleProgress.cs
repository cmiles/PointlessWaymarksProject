namespace PointlessWaymarks.CommonTools;

public class ConsoleProgress : IProgress<string>
{
    public void Report(string value)
    {
        Console.WriteLine(value);
    }
}