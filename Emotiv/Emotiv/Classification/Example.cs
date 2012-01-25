using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv;

namespace MCAEmotiv.Classification
{
    /// <summary>
    /// An example to be passed to a learning algorithm. This class is immutable.
    /// </summary>
    [Serializable]
    public struct Example
    {
        /// <summary>
        /// Contains the data in the example.
        /// </summary>
        public IArrayView<double> Features { get; private set; }

        /// <summary>
        /// The class of this example.
        /// </summary>
        public int Class { get; private set; }

        /// <summary>
        /// Construct an example with the specified class and feature data. Makes a copy of features.
        /// To preserve immutability.
        /// </summary>
        public Example(int cls, IEnumerable<double> features)
            : this(cls, features.ToIArray())
        {
        }

        private Example(int cls, IArrayView<double> features)
            : this()
        {
            this.Class = cls;
            this.Features = features;
        }

        /// <summary>
        /// Efficiently returns an identical example except with the new class value.
        /// </summary>
        public Example WithClass(int cls)
        {
            return new Example(cls, this.Features);
        }

        /// <summary>
        /// Efficiently returns an identical example except with only the features
        /// at the indices specified by the argument. No copying of features is performed.
        /// </summary>
        public Example WithFeatures(IArrayView<int> featureIndices)
        {
            return new Example(this.Class, this.Features.Select(featureIndices));
        }

        /// <summary>
        /// Efficiently returns an identical example with features that have been z-scored using means and standardDeviations.
        /// </summary>
        public Example ZScored(IArrayView<double> means, IArrayView<double> standardDeviations)
        {
            var features = this.Features;
            return new Example(this.Class, Arrays.FromMap(i => (features[i] - means[i]) / standardDeviations[i], features.Count));
        }

        /// <summary>
        /// Zscores the examples, returning the calculated means and standard deviations as out parameters.
        /// </summary>
        internal static IArrayView<Example> ZScored(IArrayView<Example> examples, 
            out IArrayView<double> means, 
            out IArrayView<double> standardDeviations)
        {
            // compute means and standard deviations
            int numFeatures = examples[0].Features.Count;
            IArray<double> meansArray = Arrays.New<double>(numFeatures), standardDeviationsArray = Arrays.New<double>(numFeatures);
            double mean;
            for (int i = 0; i < numFeatures; i++)
            {
                standardDeviationsArray[i] = examples.Select(e => e.Features[i]).StandardDeviation(out mean);
                meansArray[i] = mean;
            }
            means = meansArray;
            standardDeviations = standardDeviationsArray;

            // z-score the examples
            return examples.Select(e => e.ZScored(meansArray, standardDeviationsArray)).ToIArray();
        }
    }

    /// <summary>
    /// Contains utilities and extensions for the Example class
    /// </summary>
    public static class Examples
    {
        /// <summary>
        /// ZScores the examples, returning the calculated means and standard deviations as out parameters
        /// </summary>
        public static IArrayView<Example> ZScored(this IArrayView<Example> examples,
            out IArrayView<double> means,
            out IArrayView<double> standardDeviations)
        {
            return Example.ZScored(examples, out means, out standardDeviations);
        }
    }
}
