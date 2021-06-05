using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PointlessWaymarks.CmsData
{
    /// <summary>
    ///     Extensions methods for IEnumerable and IAsyncEnumerable to do parallel for-each loop in async-await manner
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ParallelForEachExtensions
    {
        public static async Task AsyncParallelForEach(this List<Func<Task>> source,
            int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded, TaskScheduler scheduler = null)
        {
            await source.ToListAsync().AsyncParallelForEach(async x => await x(), maxDegreeOfParallelism, scheduler);
        }

        public static async Task AsyncParallelForEach<T>(this List<T> source, Func<T, Task> body,
            int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded, TaskScheduler scheduler = null)
        {
            await source.ToListAsync().AsyncParallelForEach(body, maxDegreeOfParallelism, scheduler);
        }

        public static async Task AsyncParallelForEach<T>(this IAsyncEnumerable<T> source, Func<T, Task> body,
            int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded, TaskScheduler scheduler = null)
        {
            //Code and excellent article here
            //https://medium.com/@alex.puiu/parallel-foreach-async-in-c-36756f8ebe62
            //https://scatteredcode.net/parallel-foreach-async-in-c/#:~:text=Foreach%20itself%20is%20very%20useful,high%20latency%20or%20long%20processing.

            var options = new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = maxDegreeOfParallelism};
            if (scheduler != null)
                options.TaskScheduler = scheduler;
            var block = new ActionBlock<T>(body, options);
            await foreach (var item in source)
                block.Post(item);
            block.Complete();
            await block.Completion;
        }

        private static async IAsyncEnumerable<T> ToListAsync<T>(this List<T> toEnumerate,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < toEnumerate.Count; i++) yield return toEnumerate[i];
        }
    }
}