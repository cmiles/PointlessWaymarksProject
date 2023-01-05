using FluentMigrator.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointlessWaymarks.Task.PublishSiteToAmazonS3
{
    public class PublishSiteToAmazonS3Settings
    {
        [Required(ErrorMessage = "A Settings file for a Pointless Waymarks CMS Site must be specified.")]
        public string PointlessWaymarksSiteSettingsFileFullName { get; set; } = string.Empty;
    }
}
