using CommunityToolkit.Mvvm.Messaging.Messages;

namespace PointlessWaymarks.GeoToolsGui.Messages;

public class ExifToolSettingsUpdateMessage((object sender, string exifToolFullName) message)
    : ValueChangedMessage<(object sender, string exifToolFullName)>(message);