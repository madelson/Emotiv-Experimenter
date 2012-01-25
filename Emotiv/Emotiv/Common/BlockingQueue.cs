using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace System.Collections.Generic
{
    /// <summary>
    /// Implements a simple, thread-safe, FIFO blocking queue with unbounded capacity
    /// </summary>
    public class BlockingQueue<T>
    {
        private readonly Queue<T> queue = new Queue<T>();

        /// <summary>
        /// The number of elements in the queue
        /// </summary>
        public int Count
        {
            get
            {
                lock (this.queue)
                    return this.queue.Count;
            }
        }

        /// <summary>
        /// Add item to the end of the queue
        /// </summary>
        public void Enqueue(T item)
        {
            lock (this.queue)
            {
                this.queue.Enqueue(item);
                if (this.queue.Count == 1)
                    Monitor.Pulse(this.queue);
            }
        }

        /// <summary>
        /// Block until an item is available, then remove and return that item
        /// </summary>
        public T Dequeue()
        {
            lock (this.queue)
            {
                while (this.queue.Count == 0)
                    Monitor.Wait(this.queue);

                return this.queue.Dequeue();
            }
        }

        /// <summary>
        /// Block for up to the specified wait time or until an item is available. Removes the item and returns
        /// true if an item became available before the timeout. Returns false otherwise
        /// </summary>
        public bool TryDequeue(int maxWaitTimeMillis, out T item)
        {
            lock (this.queue)
            {
                if (this.queue.Count == 0)
                    Monitor.Wait(this.queue, maxWaitTimeMillis);
                if (this.queue.Count == 0)
                {
                    item = default(T);
                    return false;
                }

                item = this.queue.Dequeue();
                return true;
            }
        }
    }
}
