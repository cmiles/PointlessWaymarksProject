using FluentMigrator.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointlessWaymarks.Task.PhotoPickup
{
    public class PhotoPickupSettings
    {
        [Required(ErrorMessage = "The directory to look in for jpg photos in.")]
        public string PhotoPickupDirectory { get; set; }

        [Required(ErrorMessage = "A Settings file for a Pointless Waymarks CMS Site must be specified.")]
        public string PointlessWaymarksSiteSettingsFileFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "A directory to move processed Photo files into.")]
        public string PhotoPickupArchiveDirectory { get; set; } = string.Empty;

        public bool ShowInMainSiteFeed { get; set; }
    }
}
