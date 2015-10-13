using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncResetEvents
{
    /// <summary>
    /// Provides a TPL compatible version of <see cref="ManualResetEvent"/>.
    /// </summary>
    public sealed class AsyncManualResetEvent
    {
        private volatile bool _signaled;
        private readonly Dictionary<int, TaskCompletionSource<object>> _threadWaiters = new Dictionary<int, TaskCompletionSource<object>>();

        /// <summary>
        /// Creates a <see cref="Task"/> that waits until triggered via <see cref="Set"/> method.
        /// </summary>
        /// <returns>A <see cref="Task"/> instance tied to calling thread.</returns>
        public Task Wait()
        {
            lock (_threadWaiters)
            {
                TaskCompletionSource<object> taskCompletionSource;
                if (_signaled)
                {
                    taskCompletionSource = new TaskCompletionSource<object>();
                    taskCompletionSource.SetResult(null);
                }
                else
                {
                    // We are using only one Task per thread because we want to reuse the same Task in case Wait is called multiple
                    // times from the same thread. This can happen if anyone does something like
                    // 
                    // while (IsRunning) {
                    //   SomeCalculation();
                    //   await Task.Delay(TimeSpan.FromSeconds(2), asyncAutoResetEvent.Wait());
                    // }
                    // 
                    // But ideally WaitOne will be called only once per thread anyway.
                    int threadId = Thread.CurrentThread.ManagedThreadId;
                    if (!_threadWaiters.TryGetValue(threadId, out taskCompletionSource))
                    {
                        taskCompletionSource = new TaskCompletionSource<object>();
                        _threadWaiters.Add(threadId, taskCompletionSource);
                    }
                }
                
                return taskCompletionSource.Task;
            }
        }

        /// <summary>
        /// Signals all <see cref="Task"/> objects created via <see cref="Wait"/> to complete.
        /// </summary>
        public void Set()
        {
            lock (_threadWaiters)
            {
                foreach (var toSet in _threadWaiters.Values)
                {
                    toSet.SetResult(null);
                }
                _threadWaiters.Clear();

                _signaled = true;
            }
        }

        /// <summary>
        /// Resets the state of the event to non-signaled.
        /// </summary>
        public void Reset()
        {
            _signaled = false;
        }
    }
}