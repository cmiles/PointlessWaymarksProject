using System.IO.Enumeration;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CloudBackupData;

public static class LocalFiles
{
    public static async Task<List<S3LocalFileAndMetadata>> Files(BackupJob job)
    {
        var directories = await LocalDirectories.Directories(job);

        var context = await CloudBackupContext.CreateInstance();

        var excludedPatterns = await context.ExcludedFileNamePatterns.Where(x => x.JobId == job.Id)
            .Select(x => x.Pattern).OrderBy(x => x)
            .ToListAsync();

        var returnList = new List<S3LocalFileAndMetadata>();

        foreach (var directoryInfo in directories)
        {
            var files = directoryInfo.GetFiles();

            foreach (var fileInfo in files)
            {
                if (excludedPatterns.Any(x => FileSystemName.MatchesSimpleExpression(x, fileInfo.Name))) continue;
                returnList.Add(await S3Tools.LocalFileAndMetadata(fileInfo));
            }
        }

        return returnList;
    }
}