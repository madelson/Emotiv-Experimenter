using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    #region ---- Interfaces ----
    /// <summary>
    /// An interface for a read-only array-like class.
    /// </summary>
    public interface IArrayView<out T> : IEnumerable<T>
    {
        /// <summary>
        /// Get the element at index
        /// </summary>
        T this[int index] { get; }

        /// <summary>
        /// The number of elements in the array
        /// </summary>
        int Count { get; }
    }

    /// <summary>
    /// An interface for an array-like class.
    /// </summary>
    public interface IArray<T> : IArrayView<T>
    {
        /// <summary>
        /// Get or set the element at index
        /// </summary>
        new T this[int index] { get; set; }
    }
    #endregion

    #region ---- Extensions ----
    /// <summary>
    /// Provides utilities and extensions for IArray and IArrayView
    /// </summary>
    public static class Arrays
    {
        /// <summary>
        /// Create a new array of with the specified number of elements
        /// </summary>
        public static IArray<T> New<T>(int count)
        {
            return new T[count].AsIArray();
        }

        /// <summary>
        /// Create an array containing the specified number of copies of the item. Uses constant space
        /// </summary>
        public static IArrayView<T> NCopies<T>(this T item, int count)
        {
            return new NCopiesArrayView<T>(item, count);
        }

        private static void CheckBounds<T>(this IArrayView<T> array, int index)
        {
            if (index < 0 || index >= array.Count)
                throw new IndexOutOfRangeException(index.ToString());
        }

        /// <summary>
        /// Provides an IArray view of the .NET array
        /// </summary>
        public static IArray<T> AsIArray<T>(this T[] array)
        {
            return new ArrayBackedArray<T>(array);
        }

        /// <summary>
        /// Provides an IArray view of the .NET list
        /// </summary>
        public static IArray<T> AsIArray<T>(this IList<T> list)
        {
            return new ListBackedArray<T>(list);
        }

        /// <summary>
        /// Binds the enumerable to an IArrayView, casting rather than copying if possible.
        /// </summary>
        public static IArrayView<T> AsIArray<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable is T[])
                return ((T[])enumerable).AsIArray();
            if (enumerable is List<T>)
                return ((List<T>)enumerable).AsIArray();
            if (enumerable is IArrayView<T>)
                return (IArrayView<T>)enumerable;
            return enumerable.ToIArray();
        }

        /// <summary>
        /// Equivalent to <code>enumerable.ToArray().AsIArray()</code>
        /// </summary>
        public static IArray<T> ToIArray<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.ToArray().AsIArray();
        }

        /// <summary>
        /// Flattens a 2-dimensional array into a single sequence
        /// </summary>
        public static IEnumerable<T> Concatenated<T>(this IEnumerable<IArrayView<T>> arrays)
        {
            return arrays.SelectMany(a => a);
        }

        /// <summary>
        /// Returns a view of the specified portion of the array. Does not perform copying
        /// </summary>
        public static IArrayView<T> SubView<T>(this IArrayView<T> array, int start, int count)
        {
            if (start == 0 && count == array.Count)
                return array;

            return new ShallowSubArrayView<T, IArrayView<T>>(array, start, count);
        }

        /// <summary>
        /// As <code>array.SubView(start, array.Count)</code>
        /// </summary>
        public static IArrayView<T> SubView<T>(this IArrayView<T> array, int start)
        {
            return array.SubView(start, array.Count - start);
        }

        /// <summary>
        /// As SubView, but returns an IArray
        /// </summary>
        public static IArray<T> SubArray<T>(this IArray<T> array, int start, int count)
        {
            if (start == 0 && count == array.Count)
                return array;

            return new ShallowSubArray<T>(array, start, count);
        }

        /// <summary>
        /// As <code>array.SubArray(start, array.Count)</code>
        /// </summary>
        public static IArray<T> SubArray<T>(this IArray<T> array, int start)
        {
            return array.SubArray(start, array.Count - start);
        }

        /// <summary>
        /// Returns a range of the indices of the array
        /// </summary>
        public static IArrayView<int> Indices<T>(this IArrayView<T> arrayView)
        {
            return arrayView.Count.CountTo();
        }

        /// <summary>
        /// Returns the array of integers in [0, number)
        /// </summary>
        public static IArrayView<int> CountTo(this int number)
        {
            return Range(0, Math.Sign(number), Math.Abs(number));
        }

        /// <summary>
        /// Returns the array of intergers in [start, start + incr ... start + count * incr)
        /// </summary>
        public static IArrayView<int> Range(int start, int incr, int count)
        {
            return new RangeArrayView(start, incr, count);
        }

        /// <summary>
        /// Returns a read-only array of the specified length where array[index] = map[index]
        /// </summary>
        public static IArrayView<T> FromMap<T>(Func<int, T> map, int count)
        {
            return new MappedArrayView<T>(map, count);
        }

        /// <summary>
        /// Transposes the 2-dimensional array in time proportional to one of the dimensions
        /// </summary>
        public static IArrayView<IArrayView<T>> Transposed<T>(this IArrayView<IArrayView<T>> array2d)
        {
            if (array2d.Count == 0)
                return Arrays.NCopies<IArrayView<T>>(null, 0);

            return array2d[0]
                .Indices()
                .Select(i => Arrays.FromMap(j => array2d[j][i], array2d.Count))
                .ToIArray();
        }

        /// <summary>
        /// Fills the array with the specified value
        /// </summary>
        public static void Fill<T>(this IArray<T> array, T value)
        {
            for (int i = 0; i < array.Count; i++)
                array[i] = value;
        }

        /// <summary>
        /// Starting at index 0, fills array with values until they run out.
        /// </summary>
        public static void Fill<T>(this IArray<T> array, IEnumerable<T> values)
        {
            int i = 0;
            foreach (var value in values)
                array[i++] = value;
        }

        /// <summary>
        /// Implements an in-place shuffle.
        /// </summary>
        public static void Shuffle<T>(this IArray<T> array, Random randomToUse = null)
        {
            var rand = randomToUse ?? new Random();

            T temp;
            for (int i = 0, newLoc = rand.Next(array.Count); i < array.Count; i++, newLoc = rand.Next(array.Count))
            {
                temp = array[i];
                array[i] = array[newLoc];
                array[newLoc] = temp;
            }
        }

        /// <summary>
        /// Fetches the first item without invoking the enumerator.
        /// </summary>
        public static T FirstItem<T>(this IArrayView<T> array)
        {
            return array[0];
        }

        /// <summary>
        /// Fetches the last item without invoking the enumerator.
        /// </summary>
        public static T LastItem<T>(this IArrayView<T> array)
        {
            return array[array.Count - 1];
        }

        /// <summary>
        /// Returns an array containing only the elements in the current array which reside at
        /// the specified indices. Does not require copying
        /// </summary>
        public static IArrayView<T> Select<T>(this IArrayView<T> array, IArrayView<int> indices)
        {
            return new IndexSelectorArrayView<T>(array, indices);
        }

        /// <summary>
        /// As Select, but returns an array. Does not require copying
        /// </summary>
        public static IArrayView<Out> SelectArray<T, Out>(this IArrayView<T> array, Func<T, Out> selector)
        {
            return FromMap(i => selector(array[i]), array.Count);
        }

        /// <summary>
        /// As Select, but returns an array. Does not require copying
        /// </summary>
        public static IArrayView<Out> SelectArray<T, Out>(this IArrayView<T> array, Func<T, int, Out> selector)
        {
            return FromMap(i => selector(array[i], i), array.Count);
        }

        #region ---- Implementations ----
        [Serializable]
        private class RangeArrayView : AbstractEnumerable<int>, IArrayView<int>
        {
            private readonly int start, count, incr;

            public RangeArrayView(int start, int incr, int count)
            {
                this.start = start;
                this.incr = incr;
                this.count = count;
            }

            public override IEnumerator<int> GetEnumerator()
            {
                for (int i = 0, sum = this.start; i < this.Count; i++, sum += this.incr)
                    yield return sum;
            }

            public int this[int index]
            {
                get { this.CheckBounds(index); return this.start + (index * this.incr); }
            }

            public int Count
            {
                get { return this.count; }
            }
        }

        [Serializable]
        private struct NCopiesArrayView<T> : IArrayView<T>
        {
            private readonly int count;
            private readonly T item;

            public NCopiesArrayView(T item, int count)
            {
                this.item = item;
                this.count = count;
            }

            public IEnumerator<T> GetEnumerator()
            {
                for (int i = 0; i < this.Count; i++)
                    yield return this.item;
            }

            public T this[int index]
            {
                get { this.CheckBounds(index); return this.item; }
            }

            public int Count
            {
                get { return this.count; }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        [Serializable]
        private class ArrayBackedArray<T> : AbstractEnumerable<T>, IArray<T>
        {
            private readonly T[] array;

            public ArrayBackedArray(T[] array)
            {
                this.array = array;
            }

            public T this[int index]
            {
                get { return this.array[index]; }
                set { this.array[index] = value; }
            }

            public int Count
            {
                get { return this.array.Length; }
            }

            public override IEnumerator<T> GetEnumerator()
            {
                IEnumerable<T> enumerable = this.array;
                return enumerable.GetEnumerator();
            }
        }

        [Serializable]
        private class ListBackedArray<T> : AbstractEnumerable<T>, IArray<T>
        {
            private readonly IList<T> list;

            public ListBackedArray(IList<T> list)
            {
                this.list = list;
            }

            public T this[int index]
            {
                get { return this.list[index]; }
                set { this.list[index] = value; }
            }

            public int Count
            {
                get { return this.list.Count; }
            }

            public override IEnumerator<T> GetEnumerator()
            {
                return this.list.GetEnumerator();
            }
        }

        [Serializable]
        private class MappedArrayView<T> : AbstractEnumerable<T>, IArrayView<T>, ISerializable
        {
            private readonly Func<int, T> map;
            private readonly int count;

            public MappedArrayView(Func<int, T> selector, int count)
            {
                this.map = selector;
                this.count = count;
            }

            public override IEnumerator<T> GetEnumerator()
            {
                for (int i = 0; i < this.Count; i++)
                    yield return this[i];
            }

            public T this[int index]
            {
                get { this.CheckBounds(index); return this.map(index); }
            }

            public int Count
            {
                get { return this.count; }
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("array", this.ToArray(), typeof(T[]));
            }

            protected MappedArrayView(SerializationInfo info, StreamingContext context)
            {
                T[] array = (T[])info.GetValue("array", typeof(T[]));

                this.map = i => array[i];
                this.count = array.Length;
            }
        }

        [Serializable]
        private class IndexSelectorArrayView<T> : AbstractEnumerable<T>, IArrayView<T>
        {
            private readonly IArrayView<int> indices;
            private readonly IArrayView<T> source;

            public IndexSelectorArrayView(IArrayView<T> source, IArrayView<int> indices)
            {
                this.indices = indices;
                this.source = source;
            }

            public T this[int index]
            {
                get
                {
                    return this.source[this.indices[index]];
                }
            }

            public int Count
            {
                get { return this.indices.Count; }
            }

            public override IEnumerator<T> GetEnumerator()
            {
                for (int i = 0; i < this.Count; i++)
                    yield return this[i];
            }
        }

        [Serializable]
        private class ShallowSubArrayView<T, V> : AbstractEnumerable<T>, IArrayView<T>
            where V : IArrayView<T>
        {
            protected readonly V sourceArray;
            protected readonly int start, count;

            public ShallowSubArrayView(V sourceArray, int start, int count)
            {
                if (count < 0)
                    throw new ArgumentOutOfRangeException(count.ToString());
                else if (count > 0)
                {
                    sourceArray.CheckBounds(start);
                    sourceArray.CheckBounds(start + count - 1);
                }
                // we allow start to be source.Count only if count is 0
                else if (start > sourceArray.Count)
                    throw new ArgumentOutOfRangeException("start");

                this.sourceArray = sourceArray;
                this.start = start;
                this.count = count;
            }

            public override IEnumerator<T> GetEnumerator()
            {
                for (int i = this.start; i < this.start + this.count; i++)
                    yield return this.sourceArray[i];
            }

            public T this[int index]
            {
                get
                {
                    this.CheckBounds(index);
                    return this.sourceArray[index + this.start];
                }
            }

            public int Count
            {
                get { return this.count; }
            }
        }

        [Serializable]
        private class ShallowSubArray<T> : ShallowSubArrayView<T, IArray<T>>, IArray<T>
        {
            public ShallowSubArray(IArray<T> sourceArray, int start, int count)
                : base(sourceArray, start, count)
            {
            }

            public new T this[int index]
            {
                get
                {
                    return base[index];
                }
                set
                {
                    this.CheckBounds(index);
                    this.sourceArray[index + this.start] = value;
                }
            }
        }
        #endregion
    }
    #endregion
}
