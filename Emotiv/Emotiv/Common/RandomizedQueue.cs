using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.Common
{
    public class RandomizedQueue<T> : AbstractEnumerable<T>, ICollection<T>
    {
        private readonly IList<T> items = new List<T>();
        public Random Random { get; private set; }

        public RandomizedQueue(Random random)
        {
            this.Random = random;
        }

        public RandomizedQueue()
            : this(new Random())
        {
        }

        public void Add(T item)
        {
            this.items.Add(item);
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                this.Add(item);
            }
        }

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

        public void Clear()
        {
            this.items.Clear();
        }

        public bool Contains(T item)
        {
            return this.items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            return this.items.Remove(item);
        }

        public override IEnumerator<T> GetEnumerator()
        {
            return this.items.GetEnumerator();
        }
    }
}
