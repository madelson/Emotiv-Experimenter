using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.Common;

namespace MCAEmotiv.Testing.Unit
{
    class RandomizedQueueTests
    {
        public static void Run()
        {
            Test<int>(null);
            Test(null, 4);
            Test(null, 1, 2, 3);
            Test(null, 1, 1, 1, 1, 1, 1);
            var outputs = Test(new Random(0xbeef), "", (string)null, "A", "dog", "emotiv.txt", "test");
            var outputs2 = Test(new Random(0xbeef), "", (string)null, "A", "dog", "emotiv.txt", "test");
            var outputs3 = Test(new Random(0xbeef2), "", (string)null, "A", "dog", "emotiv.txt", "test");
            if (!outputs.SequenceEqual(outputs2))
            {
                throw new Exception("Should behave the same with the same seed!");
            }
            if (outputs.SequenceEqual(outputs3))
            {
                throw new Exception("Should probably behave differently with different seeds!");
            }
        }

        private static IEnumerable<T> Test<T>(Random random, params T[] inputs)
        {
            var rq = random != null ? new RandomizedQueue<T>(random) : new RandomizedQueue<T>();
            inputs.Take(3).ForEach(t => rq.Add(t));
            rq.AddRange(inputs.Skip(3));

            var outputs = new List<T>();
            for (int i = inputs.Count(); i >= 0; i--)
            {
                if (rq.Count != i)
                {
                    throw new Exception("Bad count!");
                }
                if (i > 0)
                {
                    outputs.Add(rq.RemoveRandom());
                }
                else
                {
                    var threw = false;
                    try { rq.RemoveRandom(); }
                    catch (Exception) { threw = true; }
                    if (!threw)
                    {
                        throw new Exception("Didn't throw exception when empty!");
                    }
                }
            }

            if (!inputs.OrderBy(t => t == null ? 0 : t.GetHashCode())
                .SequenceEqual(outputs.OrderBy(t => t == null ? 0 : t.GetHashCode())))
            {
                throw new Exception("Bad sequence!");
            }

            rq.AddRange(inputs);
            rq.AddRange(inputs);
            inputs.ForEach(t => rq.Remove(t));
            if (!inputs.OrderBy(t => t == null ? 0 : t.GetHashCode())
                .SequenceEqual(rq.OrderBy(t => t == null ? 0 : t.GetHashCode())))
            {
                throw new Exception("Bad sequence!");
            }

            return outputs;
        }
    }
}
