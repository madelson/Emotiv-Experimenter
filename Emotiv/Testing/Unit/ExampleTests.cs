using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.Classification;

namespace MCAEmotiv.Testing.Unit
{
    static class ExampleTests
    {
        public static void Run()
        {
            var ex = new Example(2, new double[] { 1, 3, 2 });

            // WithClass test
            if (ex.WithClass(5).Class != 5 || !ex.WithClass(5).Features.SequenceEqual(new double[] { 1, 3, 2 }))
                throw new Exception("WithClass failed");

            // ZScore test
            var examples = new Example[] {
                new Example(1, new double[] { 1, 7 }),
                new Example(3, new double[] { 5, 5 }),
                new Example(1, new double[] { 3, 3 }),
            }.AsIArray();

            IArrayView<double> means, sds;
            var zscored = examples.ZScored(out means, out sds);
            if (!means.SequenceEqual(new double[] { 3, 5 }))
                throw new Exception("Bad means");
            if (!sds.SequenceEqual(new double[] { 2, 2 }))
                throw new Exception("Bad standard deviations");
            foreach (int i in examples.Indices())
                foreach (int j in examples[0].Features.Indices())
                    if (zscored[i].Features[j] != (examples[i].Features[j] - means[j]) / sds[j])
                        throw new Exception("Bad zscore value");
        }
    }
}
