namespace PointlessWaymarks.CommonTools;

public static class FileLoadTools
{
    public static string ReadAllText(string file)
    {
        using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var textReader = new StreamReader(fileStream);
        return textReader.ReadToEnd();
    }
}