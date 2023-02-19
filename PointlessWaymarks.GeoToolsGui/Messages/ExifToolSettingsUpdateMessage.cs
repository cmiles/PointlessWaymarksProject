using CommunityToolkit.Mvvm.Messaging.Messages;

namespace PointlessWaymarks.GeoToolsGui.Messages;

public class ExifToolSettingsUpdateMessage : ValueChangedMessage<(object sender, string exifToolFullName)>
{
    public ExifToolSettingsUpdateMessage((object sender, string exifToolFullName) message) : base(message)
    {
    }
}