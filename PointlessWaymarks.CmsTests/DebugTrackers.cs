using System.Diagnostics;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsTests
{
    public static class DebugTrackers
    {
        public static void DataNotificationDiagnostic(object sender, TinyMessageReceivedEventArgs e)
        {
            Debug.Print(e.Message.ToString());
        }

        public static IProgress<string> DebugProgressTracker()
        {
            var toReturn = new Progress<string>();
            toReturn.ProgressChanged += DebugProgressTrackerChange;
            return toReturn;
        }

        private static void DebugProgressTrackerChange(object sender, string e)
        {
            Debug.WriteLine(e);
        }
    }
}