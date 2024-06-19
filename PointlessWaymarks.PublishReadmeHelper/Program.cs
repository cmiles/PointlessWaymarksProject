var directory = string.Empty;

if (args.Length < 1 || string.IsNullOrEmpty(args[0]))
{
    directory = Environment.CurrentDirectory;
}

var searchDirectory = new DirectoryInfo(directory);

Console.Write($"Pointless Waymarks README.md -> Project Specific README_[project] running in {searchDirectory.FullName}");

if (!searchDirectory.Exists)
{
    Console.WriteLine($"Directory '{directory}' could not be found - quitting.");
    return;
}

var subDirectories = searchDirectory.GetDirectories("*", SearchOption.AllDirectories);

Console.WriteLine($"Scanning {subDirectories.Length} SubDirectories.");

foreach (var subDirectory in subDirectories)
{
    try
    {
        var possibleReadme = new FileInfo(Path.Combine(subDirectory.FullName, "README.md"));

        Console.WriteLine($"Looking for {possibleReadme.FullName}");

        if (!possibleReadme.Exists)
        {
            Console.WriteLine($"No README.md found in {subDirectory.FullName} - continuing");
            continue;
        }

        var postPendValue = string.Join("-", subDirectory.Name.Split(".")[1..]);

        var targetReadme = new FileInfo(Path.Combine(subDirectory.FullName, $"README_{postPendValue}.md"));

        possibleReadme.CopyTo(targetReadme.FullName, true);

        Console.WriteLine($"Copied {possibleReadme.FullName} to {targetReadme.FullName}");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error - continuing...{Environment.NewLine}{e}");
    }

}
