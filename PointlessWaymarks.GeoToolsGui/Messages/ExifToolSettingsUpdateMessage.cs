using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointlessWaymarks.GeoToolsGui.Messages
{
    public class ExifToolSettingsUpdateMessage : ValueChangedMessage<(object sender, string exifToolFullName)>
    {
        public ExifToolSettingsUpdateMessage((object sender, string exifToolFullName) message) : base(message)
        {
        }
    }
}
