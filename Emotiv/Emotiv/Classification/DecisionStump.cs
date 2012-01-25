using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.Classification
{
    /// <summary>
    /// A basic classifier which uses only a single feature of the data
    /// </summary>
    [Serializable]
    [Description("A basic classifier which uses only a single feature of the data", DisplayName = "Decision Stump")]
    public class DecisionStump : AbstractBinaryClassifier, IWeightedClassifier
    {
        /// <summary>
        /// The index of the feature to use
        /// </summary>
        [Parameter("The index of the feature which is used for classification", DefaultValue = 0, MinValue = 0)]
        public int Feature { get; set; }

        private double cutPoint;
        private bool negativesBelowCutPoint;

        /// <summary>
        /// Returns -1
        /// </summary>
        public override int NegativeExampleValue
        {
            get { return -1; }
        }

        /// <summary>
        /// Returns 1
        /// </summary>
        public override int PositiveExampleValue
        {
            get { return +1; }
        }

        /// <summary>
        /// Trains the classifier
        /// </summary>
        protected override void TrainBinary(IArrayView<Example> binaryExamples)
        {
            this.TrainBinary(binaryExamples, Arrays.NCopies(1.0 / binaryExamples.Count, binaryExamples.Count));
        }

        /// <summary>
        /// Classifies the example
        /// </summary>
        protected override int PredictBinary(Example binaryExample, out double confidence)
        {
            int feature = this.Feature < binaryExample.Features.Count ? this.Feature : 0;

            confidence = 1.0;
            return ((binaryExample.Features[feature] < this.cutPoint) == this.negativesBelowCutPoint)
                ? this.NegativeExampleValue
                : this.PositiveExampleValue;
        }

        /// <summary>
        /// Returns false
        /// </summary>
        public override bool ComputesConfidence
        {
            get { return false; }
        }

        /// <summary>
        /// Trains the classifier using weighted examples
        /// </summary>
        public void Train(IArrayView<Example> labeledExamples, IArrayView<double> weights)
        {
            this.TrainBinary(labeledExamples.SelectArray(this.ConvertToBinaryExample), weights);
            this.IsTrained = true;
        }

        private void TrainBinary(IArrayView<Example> binaryExamples, IArrayView<double> weights)
        {
            int feature = this.Feature < binaryExamples[0].Features.Count ? this.Feature : 0;
            var sortedExamples = binaryExamples.Select((e, i) => new { Example = e, Weight = weights[i] }).ToList();
            sortedExamples.Sort((e1, e2) => e1.Example.Features[feature].CompareTo(e2.Example.Features[feature]));

            // init the errors
            double negsBelowError = 0, negsAboveError = 0;
            foreach (var e in sortedExamples)
                if (e.Example.Class == this.NegativeExampleValue)
                    negsBelowError += e.Weight;
                else
                    negsAboveError += e.Weight;

            // determine the optimal cut point
            double bestError = Math.Min(negsBelowError, negsAboveError);
            bool bestPolicy = negsBelowError < negsAboveError;
            int bestCutPoint = 0;
            for (int cutPoint = 1; cutPoint <= sortedExamples.Count; cutPoint++)
            {
                if (sortedExamples[cutPoint - 1].Example.Class == this.NegativeExampleValue)
                {
                    negsBelowError -= sortedExamples[cutPoint - 1].Weight;
                    negsAboveError += sortedExamples[cutPoint - 1].Weight;
                }
                else
                {
                    negsBelowError += sortedExamples[cutPoint - 1].Weight;
                    negsAboveError -= sortedExamples[cutPoint - 1].Weight;
                }

                if (negsBelowError < bestError)
                {
                    bestError = negsBelowError;
                    bestPolicy = true;
                    bestCutPoint = cutPoint;
                }
                if (negsAboveError < bestError)
                {
                    bestError = negsAboveError;
                    bestPolicy = false;
                    bestCutPoint = cutPoint;
                }
            }

            // return
            if (bestCutPoint == sortedExamples.Count)
                this.cutPoint = sortedExamples.LastItem().Example.Features[feature] * 1.01 + double.Epsilon;
            else if (bestCutPoint == 0)
                this.cutPoint = sortedExamples[0].Example.Features[feature] * 0.99 - double.Epsilon;
            else
                this.cutPoint = (sortedExamples[bestCutPoint - 1].Example.Features[feature] + sortedExamples[bestCutPoint].Example.Features[feature]) / 2.0;
            this.negativesBelowCutPoint = bestPolicy;
        }
    }
}
