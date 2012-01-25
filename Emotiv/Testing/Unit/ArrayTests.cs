using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MCAEmotiv.Testing.Unit
{
    static class ArrayTests
    {
        public static void Run()
        {
            foreach (var array in GetArrays())
            {
                try
                {
                    array.TestArrayAccess();
                }
                catch (Exception e)
                {
                    e.Print();
                }
            }

            foreach (var array in GetArrayViews())
            {
                try
                {
                    array.TestArrayViewAccess();
                }
                catch (Exception e)
                {
                    e.Print();
                }

                try
                {
                    array.TestSerializable(Enumerable.SequenceEqual);
                }
                catch (Exception e)
                {
                    e.Print();
                }
            }

            try
            {
                OtherTests();
            }
            catch (Exception e)
            {
                e.Print();
            }
        }

        private static IEnumerable<IArray<int>> GetArrays()
        {
            var array = Arrays.New<int>(10);
            array.Fill(5);
            yield return array;

            yield return array.SubArray(5);
            yield return array.SubArray(4, 6);

            int[] ints = new int[] { 1, 3, 5, 7, 9, 11 };
            yield return ints.AsIArray();
            yield return new List<int>(ints).AsIArray();
        }

        private static IEnumerable<IArrayView<int>> GetArrayViews()
        {
            foreach (var array in GetArrays())
                yield return array;

            yield return (10).CountTo();
            yield return Arrays.Range(0, 2, 5);
            yield return Arrays.NCopies(3, 10);
            yield return Arrays.FromMap(i => i * i, 10);
            yield return (10).CountTo().SubView(3, 7);
            yield return (10).CountTo().SubView(3);
            yield return new int[] { 1, 3, 4, 5, 7 }.AsIArray().SelectArray(i => i * i);
            yield return new int[] { 1, 3, 4, 5, 7 }.AsIArray().SelectArray((i, index) => i * i + index);
            yield return (10).CountTo().Then(Arrays.NCopies(-1, 10)).ToIArray().Transposed()[9];
        }

        private static void TestArrayAccess<T>(this IArray<T> array)
        {
            array.TestArrayViewAccess();

            for (int i = 0; i < array.Count; i++)
            {
                array[i] = default(T);
                if (!array[i].Equals(default(T)))
                    throw new Exception(array.GetType().Name + ": set didn't stick");
            }
        }

        private static void TestArrayViewAccess<T>(this IArrayView<T> array)
        {
            var list = new List<T>();
            for (int i = 0; i < array.Count; i++)
                list.Add(array[i]);

            var list2 = new List<T>();
            foreach (T t in array)
                list2.Add(t);

            if (!list.SequenceEqual(list2))
                throw new Exception(array.GetType().Name + ": " + list.ConcatToString(',') + " != " + list2.ConcatToString(','));

            bool caught = false;
            T item;
            try
            {
                item = array[-1];
            }
            catch (Exception)
            {
                caught = true;
            }
            if (!caught)
                throw new Exception(array.GetType().Name + ": allowed -1 access");

            caught = false;
            try
            {
                item = array[array.Count];
            }
            catch (Exception)
            {
                caught = true;
            }
            if (!caught)
                throw new Exception(array.GetType().Name + ": allowed Count access");
        }

        private static void OtherTests()
        {
            // transposed
            var array2d = Arrays.NCopies(1, 3).Then(new int[] { 4, 5, 6 }.ToIArray()).Then(Arrays.FromMap(i => i * i, 3)).Then((3).CountTo()).ToIArray();
            var transposed = array2d.Transposed();
            var doubleTransposed = transposed.Transposed();
            for (int i = 0; i < array2d.Count; i++)
                for (int j = 0; j < array2d[j].Count; j++)
                    if (array2d[i][j] != transposed[j][i])
                        throw new Exception("Transposed failed");
                    else if (array2d[i][j] != doubleTransposed[i][j])
                        throw new Exception("Double Transposed failed");
        }
    }
}
