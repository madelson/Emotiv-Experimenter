using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.Common
{
    /// <summary>
    /// A collection whose elements can be removed in random order
    /// </summary>
    public class RandomizedQueue<T> : AbstractEnumerable<T>, ICollection<T>
    {
        private readonly IList<T> items = new List<T>();

        /// <summary>
        /// The random number generator used by the queue
        /// </summary>
        public Random Random { get; private set; }

        /// <summary>
        /// Creates a new queue which uses the provided random number generator for
        /// randomization
        /// </summary>
        public RandomizedQueue(Random random)
        {
            this.Random = random;
        }

        /// <summary>
        /// Creates a new queue whose random has a time-dependent seed
        /// </summary>
        public RandomizedQueue()
            : this(new Random())
        {
        }

        /// <summary>
        /// Add item to the queue
        /// </summary>
        public void Add(T item)
        {
            this.items.Add(item);
        }

        /// <summary>
        /// Add each item to the queue
        /// </summary>
        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                this.Add(item);
            }
        }

        /// <summary>
        /// Removes and returns a random item from the queue. Throws an exception if the queue is empty
        /// </summary>
        public T RemoveRandom()
        {
            if (this.items.Count == 0)
            {
                throw new Exception("There are no items in the randomized queue!");
            }

            var selection = this.Random.Next(this.items.Count);
            var item = this.items[selection];
            this.items[selection] = this.items[this.items.Count - 1];
            this.items.RemoveAt(this.items.Count - 1);

            return item;
        }

        /// <summary>
        /// Removes all items from the queue
        /// </summary>
        public void Clear()
        {
            this.items.Clear();
        }

        /// <summary>
        /// Does the queue contain the item?
        /// </summary>
        public bool Contains(T item)
        {
            return this.items.Contains(item);
        }

        /// <summary>
        /// Copies the queue to an array starting at the given index
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            this.items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// The number of items in the queue
        /// </summary>
        public int Count
        {
            get { return this.items.Count; }
        }

        /// <summary>
        /// Returns false
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the first occurence of item from the queue, returning true if an item
        /// was removed and false otherwise
        /// </summary>
        public bool Remove(T item)
        {
            return this.items.Remove(item);
        }

        /// <summary>
        /// Returns an enumerator over the items in the queue
        /// </summary>
        public override IEnumerator<T> GetEnumerator()
        {
            return this.items.GetEnumerator();
        }
    }
}
