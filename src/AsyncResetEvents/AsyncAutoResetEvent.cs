using System.Threading;
using System.Threading.Tasks;

namespace AsyncResetEvents
{
    /// <summary>
    /// Provides a TPL compatible version of <see cref="AutoResetEvent"/>.
    /// </summary>
    // Based on https://social.msdn.microsoft.com/Forums/en-US/a30ff364-3435-4026-9f12-9bd9776c9a01/signalling-await?forum=async
    public sealed class AsyncAutoResetEvent
    {
        private bool _signaled;
        // We are using a custom ThreadAffinityQueue because we want to reuse the same Task in case WaitOne is called multiple
        // times from the same thread. This can happen if anyone does something like
        // 
        // while (IsRunning) {
        //   SomeCalculation();
        //   await Task.Delay(TimeSpan.FromSeconds(2), asyncAutoResetEvent.WaitOne());
        // }
        // 
        // But ideally WaitOne will be called only once per thread anyway.
        private readonly ThreadAffinityQueue<TaskCompletionSource<object>> _waiters = new ThreadAffinityQueue<TaskCompletionSource<object>>();

        /// <summary>
        /// Creates a <see cref="Task"/> that waits until triggered via <see cref="Set"/> method.
        /// </summary>
        /// <returns>A <see cref="Task"/> instance tied to calling thread.</returns>
        public Task WaitOne()
        {
            lock (_waiters)
            {
                TaskCompletionSource<object> taskCompletionSource;
                if ((_waiters.Count > 0) || !_signaled)
                {
                    taskCompletionSource = _waiters.EnqueueOrGet(() => new TaskCompletionSource<object>());
                }
                else
                {
                    taskCompletionSource = new TaskCompletionSource<object>();
                    taskCompletionSource.SetResult(null);
                    _signaled = false;
                }
                return taskCompletionSource.Task;
            }
        }

        /// <summary>
        /// Signals a single <see cref="Task"/> created via <see cref="WaitOne"/> to complete.
        /// </summary>
        public void Set()
        {
            TaskCompletionSource<object> toSet = null;
            lock (_waiters)
            {
                if (_waiters.Count > 0)
                {
                    toSet = _waiters.Dequeue();
                }
                else
                {
                    _signaled = true;
                }
            }

            toSet?.SetResult(null);
        }
    }
}
