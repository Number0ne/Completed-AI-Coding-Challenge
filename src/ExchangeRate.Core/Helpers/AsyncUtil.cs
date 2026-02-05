using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRate.Core.Helpers
{
    /// <summary>
    /// Utility class for running async methods synchronously.
    /// </summary>
    public static class AsyncUtil
    {
        private static readonly TaskFactory _taskFactory = new TaskFactory(
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        /// <summary>
        /// Runs an async Task method synchronously without deadlocking.
        /// </summary>
        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return _taskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Runs an async Task method synchronously without deadlocking.
        /// </summary>
        public static void RunSync(Func<Task> func)
        {
            _taskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        ///
        /// This is a class that will convert an ienumerator to an enumerator
        /// 

        public static async Task<List<T>> Get_Enumerable_From_IEnumerable<T>(this IAsyncEnumerable<T> enumerable)
        {
            var result = new List<T>();
            await foreach (var item in enumerable)
            {
                result.Add(item);
            }
            return result;
        }
    }
}
