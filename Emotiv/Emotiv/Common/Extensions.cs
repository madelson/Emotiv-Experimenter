using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace System
{
    /// <summary>
    /// Provides general-purpose extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Computes the mean and standard deviation.
        /// </summary>
        public static double StandardDeviation(this IEnumerable<double> enumerable, out double mean)
        {
            int count = 0;
            double sum = 0, sumOfSquares = 0;
            foreach (var d in enumerable)
            {
                count++;
                sum += d;
                sumOfSquares += d * d;
            }

            mean = sum / count;
            return Math.Sqrt((sumOfSquares - (count * mean * mean)) / (count - 1));
        }

        /// <summary>
        /// Computes the exponentially-smoothed moving average of the sequence
        /// </summary>
        public static IEnumerable<double> MovingAverages(this IEnumerable<double> enumerable, double alpha)
        {
            if (alpha < 0 || alpha > 1.0)
                throw new ArgumentOutOfRangeException("alpha");

            double weightedSum = enumerable.First();
            foreach (double d in enumerable)
                yield return (weightedSum = ((1 - alpha) * weightedSum) + (alpha * d));
        }

        /// <summary>
        /// Performs the action on each item in the enumerable
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
                action(item);
        }

        /// <summary>
        /// Performs the action on each item in the enumerable as well as the item's index
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)
        {
            enumerable.Select(Tuples.New).ForEach(d => action(d.Item1, d.Item2));
        }

        /// <summary>
        /// Returns the elements of the enumerable in sorted order
        /// </summary>
        public static IEnumerable<T> InOrder<T>(this IEnumerable<T> enumerable)
            where T : IComparable<T>
        {
            return enumerable.OrderBy(t => t);
        }

        /// <summary>
        /// The returned values represent how the indices of the current sequence would change after the sequence was sorted 
        /// via the key selector
        /// </summary>
        public static IArrayView<int> IndicesWhenOrderedBy<TSource, TKey>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector)
        {
            return enumerable.Select(Tuples.New).OrderBy(d => keySelector(d.Item1)).Select(d => d.Item2).ToIArray();
        }

        /// <summary>
        /// Returns the object after printing it to standard output
        /// </summary>
        public static T Print<T>(this T obj)
        {
            Console.WriteLine(obj);

            return obj;
        }

        /// <summary>
        /// Returns the enumerable after printing its elements
        /// </summary>
        public static IEnumerable<T> PrintAll<T>(this IEnumerable<T> enumerable)
        {
            String.Concat('[', enumerable.ConcatToString(','), ']').Print();

            return enumerable;
        }

        /// <summary>
        /// Returns the sequence of delta (change) values
        /// </summary>
        public static IEnumerable<double> AsDeltas(this IEnumerable<double> enumerable)
        {
            if (enumerable.IsEmpty())
                yield break;

            double lastVal = enumerable.First();
            foreach (double val in enumerable.Skip(1))
            {
                yield return val - lastVal;
                lastVal = val;
            }
        }

        /// <summary>
        /// Checks whether the enumerable is empty in constant time
        /// </summary>
        public static bool IsEmpty<T>(this IEnumerable<T> items)
        {
            using (var iter = items.GetEnumerator())
            {
                return !iter.MoveNext();
            }
        }

        /// <summary>
        /// Concatentates the items in the enumerable to a string, formatting them with the specified format string
        /// and separating them with the specified character
        /// </summary>
        public static string ConcatToString<T>(this IEnumerable<T> items, char separator, string format = null)
        {
            return items.ConcatToString(separator.ToString(), format);
        }

        /// <summary>
        /// Concatentates the items in the enumerable to a string, formatting them with the specified format string
        /// and separating them with the specified string
        /// </summary>
        public static string ConcatToString<T>(this IEnumerable<T> items, string separator = "", string format = null)
        {
            if (items.IsEmpty())
                return string.Empty;

            StringBuilder sb;
            if (format == null)
            {
                sb = new StringBuilder(items.First().ToString());
                foreach (var item in items.Skip(1))
                    sb.Append(separator).Append(item);
            }
            else
            {
                string formatString = "{0:" + format + "}";
                sb = new StringBuilder(string.Format(formatString, items.First()));
                foreach (var item in items.Skip(1))
                    sb.Append(separator).Append(string.Format(formatString, item));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns the sequence containing all of the elements in the enumerable
        /// followed by the specified item
        /// </summary>
        public static IEnumerable<T> Then<T>(this IEnumerable<T> items, T nextItem)
        {
            return items.Concat(nextItem.Enumerate());
        }

        /// <summary>
        /// Returns the sequence containing the specified item followed by the 
        /// elements of the enumerable
        /// </summary>
        public static IEnumerable<T> Then<T>(this T item, IEnumerable<T> nextItems)
        {
            return item.Enumerate().Concat(nextItems);
        }

        /// <summary>
        /// Returns the sequence containing the specified item followed by the other specified item
        /// </summary>
        public static IEnumerable<T> Then<T>(this T item, T nextItem)
        {
            return item.Enumerate().Then(nextItem);
        }

        /// <summary>
        /// Returns a sequence containing only the specified item
        /// </summary>
        public static IEnumerable<T> Enumerate<T>(this T item)
        {
            return new T[] { item };
        }

        /// <summary>
        /// Flattens the collection of enumerables into a single sequence
        /// </summary>
        public static IEnumerable<T> Concatenated<T>(this IEnumerable<IEnumerable<T>> enumerables)
        {
            return enumerables.SelectMany(en => en);
        }

        /// <summary>
        /// Returns the square of the distance between the two vectors
        /// </summary>
        public static double SquaredDistanceTo(this IEnumerable<double> enumerable, IEnumerable<double> that)
        {
            using (IEnumerator<double> thisIter = enumerable.GetEnumerator(), thatIter = that.GetEnumerator())
            {
                double diff, squaredDist = 0.0;
                while (thisIter.MoveNext() && thatIter.MoveNext())
                {
                    diff = thisIter.Current - thatIter.Current;
                    squaredDist += diff * diff;
                }

                return squaredDist;
            }
        }

        /// <summary>
        /// Returns the inner product of the two vectors
        /// </summary>
        public static double InnerProduct(this IEnumerable<double> enumerable, IEnumerable<double> that)
        {
            using (IEnumerator<double> thisIter = enumerable.GetEnumerator(), thatIter = that.GetEnumerator())
            {
                double dotProduct = 0;
                while (thisIter.MoveNext() && thatIter.MoveNext())
                    dotProduct += thisIter.Current * thatIter.Current;

                return dotProduct;
            }
        }

        /// <summary>
        /// Returns the value if it is not infinite or NaN, or the default value otherwise
        /// </summary>
        public static double AsFinite(this double value, double defaultValue = 0)
        {
            return double.IsInfinity(value) || double.IsNaN(value)
                ? defaultValue
                : value;
        }

        /// <summary>
        /// Rounds the double to an int
        /// </summary>
        public static int Rounded(this double val)
        {
            return (int)Math.Round(val);
        }

        /// <summary>
        /// Casts the int to a double
        /// </summary>
        public static double ToDouble(this int val)
        {
            return (double)val;
        }

        /// <summary>
        /// Return the cumulative sums of the vector
        /// </summary>
        public static IEnumerable<double> Cumsums(this IEnumerable<double> enumerable)
        {
            double sum = 0;
            foreach (double d in enumerable)
                yield return (sum += d);
        }

        /// <summary>
        /// Returns the index of the maximum element of the vector
        /// </summary>
        public static int Argmax<T>(this IEnumerable<T> enumerable)
            where T : IComparable<T>
        {
            int i = 1, bestIndex = 0;
            T best = enumerable.First();
            foreach (T t in enumerable.Skip(1))
            {
                if (t.CompareTo(best) > 0)
                {
                    best = t;
                    bestIndex = i;
                }
                i++;
            }

            return bestIndex;
        }

        /// <summary>
        /// Efficiently returns the last element in array.
        /// </summary>
        public static T LastItem<T>(this T[] array)
        {
            return array[array.Length - 1];
        }

        /// <summary>
        /// Efficiently returns the last element in list.
        /// </summary>
        public static T LastItem<T>(this IList<T> list)
        {
            return list[list.Count - 1];
        }

        /// <summary>
        /// Returns a random boolean value
        /// </summary>
        public static bool NextBool(this Random rand)
        {
            return rand.Next(2) == 1;
        }

        /// <summary>
        /// Returns an array whose contents are the contents of this enumerable in a random order.
        /// </summary>
        public static IArrayView<T> Shuffled<T>(this IEnumerable<T> enumerable, Random randomToUse = null)
        {
            var array = enumerable.ToIArray();
            array.Shuffle(randomToUse);

            return array;
        }

        /// <summary>
        /// Enumerates over both sequences in parallel
        /// </summary>
        public static IEnumerable<Duo<T, V>> ParallelTo<T, V>(this IEnumerable<T> en1, IEnumerable<V> en2)
        {
            using (var iter1 = en1.GetEnumerator())
            {
                using (var iter2 = en2.GetEnumerator())
                {
                    while (iter1.MoveNext() && iter2.MoveNext())
                        yield return Tuples.New(iter1.Current, iter2.Current);
                }
            }
        }

        /// <summary>
        /// Uses reflection to instantiate a new object of type with no arguments
        /// </summary>
        public static object New(this Type type)
        {
            return type.GetConstructor(new Type[0]).Invoke(Utils.EmptyArgs);
        }

        /// <summary>
        /// Returns all types in the the types's assembly or in more assemblies which derive from type. 
        /// Does not return interface or abstract types.
        /// </summary>
        public static IArrayView<Type> GetImplementingTypes(this Type type, IEnumerable<Assembly> moreAssemblies = null)
        {
            var assemblies = moreAssemblies == null
                ? type.Assembly.Enumerate()
                : type.Assembly.Then(moreAssemblies).Distinct();

            return assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsInterface && !t.IsAbstract && type.IsAssignableFrom(t))
                .ToIArray();
        }

        /// <summary>
        /// Returns true iff the object was successfully serialized the the specified file using a binary formatter.
        /// </summary>
        public static bool TrySerializeToFile(this object obj, string path)
        {
            try
            {
                using (var stream = File.Open(path, FileMode.Create))
                    new BinaryFormatter().Serialize(stream, obj);
                return true;
            }
            catch (Exception) { return false; }
        }

        /// <summary>
        /// Indents the string by adding the indent string depth number of times at the beginning and before each newline
        /// </summary>
        public static string Indent(this string toIndent, int depth = 1, string indentString = "\t")
        {
            if (depth < 0)
                throw new ArgumentException("depth must be positive");

            string indentation = indentString.NCopies(depth).ConcatToString();

            return indentation + toIndent.Replace(Environment.NewLine, Environment.NewLine + indentation);
        }
    }
}
