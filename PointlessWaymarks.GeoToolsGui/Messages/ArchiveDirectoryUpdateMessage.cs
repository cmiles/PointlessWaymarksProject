using CommunityToolkit.Mvvm.Messaging.Messages;

namespace PointlessWaymarks.GeoToolsGui.Messages;

public class ArchiveDirectoryUpdateMessage : ValueChangedMessage<(object sender, string archiveDirectory)>
{
    public ArchiveDirectoryUpdateMessage((object sender, string archiveDirectory) message) : base(message)
    {
    }
}