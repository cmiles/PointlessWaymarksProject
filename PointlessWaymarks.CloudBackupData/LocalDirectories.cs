using System.IO.Enumeration;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData;

public static class LocalDirectories
{
    /// <summary>
    ///     Returns the Job Directory and all subdirectories that are not by the Job's Excluded Directories
    ///     or Excluded Directory Name Patterns
    /// </summary>
    /// <param name="job"></param>
    /// <returns></returns>
    public static async Task<List<DirectoryInfo>> Directories(BackupJob job)
    {
        var context = await CloudBackupContext.CreateInstance();

        var excludedDirectories = await context.ExcludedDirectories.Where(x => x.JobId == job.Id)
            .Select(x => x.Directory)
            .ToListAsync();

        var excludedDirectoryPatterns = await context.ExcludedDirectoryNamePatterns.Where(x => x.JobId == job.Id)
            .Select(x => x.Pattern).ToListAsync();

        var initialDirectory = new DirectoryInfo(job.InitialDirectory);

        return initialDirectory.AsList().Concat(Directories(new DirectoryInfo(job.InitialDirectory),
            excludedDirectories, excludedDirectoryPatterns)).ToList();
    }

    private static List<DirectoryInfo> Directories(DirectoryInfo searchDirectory, List<string> excludedDirectories,
        List<string> excludedNamePatterns)
    {
        var subDirectories = searchDirectory.GetDirectories().ToList();

        if (!subDirectories.Any()) return new List<DirectoryInfo>();

        var returnList = new List<DirectoryInfo>();

        foreach (var directoryInfo in subDirectories)
        {
            if (excludedDirectories.Contains(directoryInfo.FullName)) continue;

            if (excludedNamePatterns.Any(x => FileSystemName.MatchesSimpleExpression(x, directoryInfo.Name))) continue;

            returnList.Add(directoryInfo);

            returnList.AddRange(Directories(directoryInfo, excludedDirectories, excludedNamePatterns));
        }

        return returnList;
    }
}