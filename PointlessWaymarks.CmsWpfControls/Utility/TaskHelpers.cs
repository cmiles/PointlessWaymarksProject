using System.Threading;
using System.Threading.Tasks;

namespace PointlessWaymarks.CmsWpfControls.Utility
{
    public static class TaskHelpers
    {
        public static async Task WhenCancelled(this CancellationToken cancellationToken)
        {
            //https://github.com/dotnet/runtime/issues/14991
            var taskCompletionSource = new TaskCompletionSource<bool>();

            await using (cancellationToken.Register(() => { taskCompletionSource.TrySetResult(true); }))
            {
                await taskCompletionSource.Task;
            }
        }
    }
}