using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.Classification;

namespace MCAEmotiv.Testing.Unit
{
    static class ExtensionsTests
    {
        public static void Run()
        {
            // argmax
            if (A(-3, 2, 5, 1, 0).Argmax() != 2)
                throw new Exception("Argmax failed");

            // asdeltas
            if (!A(2.0, 5.0, -2.0).AsDeltas().SequenceEqual(A(3.0, -7.0)))
                throw new Exception("AsDeltas failed");
 
            // asenumerable
            if (!(5).Enumerate().SequenceEqual(A(5)))
                throw new Exception("AsEnumerable failed");

            // concatenated
            if (!A(A(1, 1, 1), A(1, 1, 1), A(1, 1, 1), A(1), A(1, 1), A<int>())
                .Concatenated()
                .SequenceEqual(Arrays.NCopies(1, 12)))
                throw new Exception("Concatenated failed");

            // concattostring
            if (!A(1, 2, 3, -500).ConcatToString("x").Equals("1x2x3x-500"))
                throw new Exception("ConcatToString failed");

            // cumsums
            if (!A(1.0, -1, 5, 6, -3).Cumsums().SequenceEqual(A(1.0, 0, 5, 11, 8)))
                throw new Exception("Cumsums failed");

            // indiceswhenorderedby
            if (!A(3, 2, 4, 6, 1).IndicesWhenOrderedBy(i => i).SequenceEqual(A(4, 1, 0, 2, 3)))
                throw new Exception("IndicesWhenOrderedBy failed");

            // innerproduct
            if (A(1.0, 2.0, 3.0).InnerProduct(A(2.0, 3.0, 1.0)) != 11.0)
                throw new Exception("InnerProduct failed");
 
            // inorder
            if (!A(2, 1, -6, -3, 4).InOrder().SequenceEqual(A(-6, -3, 1, 2, 4)))
                throw new Exception("InOrder failed");

            // isempty
            if (A(2).IsEmpty() || !A<string>().IsEmpty())
                throw new Exception("IsEmpty failed");

            // lastitem
            if (A(1, 2, 9).LastItem() != 9)
                throw new Exception("LastItem failed");

            // movingaverages
            var seq = A(1.0, 2.0, 1.0, 3.0);
            var avgs = seq.MovingAverages(0.5).ToArray();
            var shouldBe = A(1.0, 1.5, 1.25, 2.125);
            for (int i = 0; i < shouldBe.Length; i++)
                if (Math.Abs(avgs[i] - shouldBe[i]) > 1e-6)
                    throw new Exception("MovingAverages failed");

            // nextbool
            var rand = new Random();
            int trials = 1000000, trues = 0;
            for (int i = 0; i < trials; i++)
                if (rand.NextBool())
                    trues++;
            if (Math.Abs(trues / trials.ToDouble() - 0.5) > 1e-2)
                throw new Exception("NextBool failed");

            // parallelto
            foreach (var d in A(1, 4, 2, 3).ParallelTo(A(1, 16, 4, 9)))
                if (d.Item1 * d.Item1 != d.Item2)
                    throw new Exception("ParallelTo failed");

            // rounded
            if ((1.49).Rounded() != 1 || (1.5).Rounded() != 2)
                throw new Exception("Rounded failed");

            // shuffled
            var ints = A(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            if (ints.Shuffled().SequenceEqual(ints) &&
                ints.Shuffled(new Random(rand.Next())).SequenceEqual(ints))
                throw new Exception("Shuffled failed OR extremely unlikely event occurred!");
            if (ints.Shuffled().Count != ints.Length ||
                !ints.Shuffled().Distinct().InOrder().SequenceEqual(ints))
                throw new Exception("Shuffled failed!");

            // squareddistanceto
            if (A(1.0, 2, 3).SquaredDistanceTo(A(2.0, 1.0, -3.0)) != 38.0)
                throw new Exception("SquaredDistanceTo failed");

            // standarddeviation
            double mean;
            if (Math.Abs(A(1.0, 2.0, 3.0, 4.0, 5.0).StandardDeviation(out mean) - 1.58113883) > 1e-5)
                throw new Exception("StandardDeviation failed");

            // todouble
            if ((5).ToDouble() != 5.0)
                throw new Exception("ToDouble failed");

            // new
            if (!new List<int>().SequenceEqual((IEnumerable<int>)typeof(List<int>).New()))
                throw new Exception("New failed");

            // getimplementingtypes
            var types = typeof(IClassifier).GetImplementingTypes();
            if (!types.Contains(typeof(KNN)))
                throw new Exception("GetImplementingTypes failed");
            if (types.Contains(typeof(AbstractClassifier)))
                throw new Exception("GetImplementingTypes failed");

            // asfinite
            if (double.NaN.AsFinite(5) != double.PositiveInfinity.AsFinite(5)
                || double.NegativeInfinity.AsFinite(5) != 5)
                throw new Exception("AsFinite failed!");

            // foreach
            List<int> list1 = new List<int>(), list2 = new List<int>();
            foreach (var i in (10).CountTo())
                list1.Add(i * i - 5);
            (10).CountTo().ForEach(i => list2.Add(i * i - 5));
            if (!list1.SequenceEqual(list2))
                throw new Exception("ForEach failed");
            for (int i = 0; i < list1.Count / 2; i++)
                list1[i] *= (7 + i);
            var list2cpy = list2.ToArray();
            list2.ForEach((v, i) =>
            { 
                if (i < list2.Count / 2)
                    list2cpy[i] *= (7 + i);
            });
            if (!list1.SequenceEqual(list2cpy))
                throw new Exception("ForEach failed");

            // indent
            string toIndent = "abc" + Environment.NewLine + " faaf " + Environment.NewLine + Environment.NewLine + "m";
            string indented = toIndent.Indent(3, "---");
            string indentation = "---------";
            if (indented != indentation + "abc" + Environment.NewLine + indentation + " faaf " + Environment.NewLine + indentation + Environment.NewLine + indentation + "m")
                throw new Exception("Indent failed");
        }

        private static T[] A<T>(params T[] args)
        {
            return args;
        }
    }
}
