using System.ComponentModel.DataAnnotations;

namespace PointlessWaymarks.Task.PublishSiteToAmazonS3;

public class PublishSiteToAmazonS3Settings
{
    public static string ProgramShortName = "Publish Site to Amazon S3";

    [Required(ErrorMessage = "A Settings file for a Pointless Waymarks CMS Site must be specified.")]
    public string PointlessWaymarksSiteSettingsFileFullName { get; set; } = string.Empty;
}