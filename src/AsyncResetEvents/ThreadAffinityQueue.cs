using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AsyncResetEvents
{
    /// <summary>
    /// Represents a <see cref="Queue"/> where each thread can enqueue only one item.
    /// </summary>
    public sealed class ThreadAffinityQueue<T>: IEnumerable<T>
    {
        private readonly Queue<int> _threads = new Queue<int>();
        private readonly Dictionary<int, T> _items = new Dictionary<int, T>();

        /// <summary>
        /// Gets the number of items in the queue.
        /// </summary>
        public int Count => _threads.Count;

        /// <summary>
        /// Enqueues a new item if queue does not contain an item for current thread, otherwise returns the item already in the queue.
        /// </summary>
        /// <param name="factory">Factory method to create a new item if there is no item for current thread in queue.</param>
        /// <returns>Existing item for current thread if exists; otherwise, item returned via factory.</returns>
        public T EnqueueOrGet(Func<T> factory)
        {
            T result;
            int threadId = Thread.CurrentThread.ManagedThreadId;

            if (!_items.TryGetValue(threadId, out result))
            {
                result = factory();
                _threads.Enqueue(threadId);
                _items.Add(threadId, result);
            }
            return result;
        }

        /// <summary>
        /// Removes and returns the first item in the queue.
        /// </summary>
        public T Dequeue()
        {
            int threadId = _threads.Dequeue();
            T result = _items[threadId];
            _items.Remove(threadId);
            return result;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _threads.Select(threadId => _items[threadId]).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}