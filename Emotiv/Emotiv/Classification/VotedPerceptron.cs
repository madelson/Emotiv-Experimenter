using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.Classification
{
    /// <summary>
    /// Classifies examples according to the weighted vote of a collection of seperating hyperplance classifiers learned over several epochs
    /// </summary>
    [Serializable]
    [Description("Classifies examples according to the weighted vote of a collection of seperating hyperplance classifiers learned over several epochs",
        DisplayName = "Voted Perceptron")]
    public class VotedPerceptron : AbstractOnlineBinaryClassifier, IWeightedClassifier
    {
        /// <summary>
        /// Returns false
        /// </summary>
        public override bool ComputesConfidence { get { return false; } }

        /// <summary>
        /// The kernel function used in classification
        /// </summary>
        [Parameter("The kernel function used in classification", DefaultValue = typeof(BasicKernel))]
        public IKernel Kernel { get; set; }

        /// <summary>
        /// The number of iterations over ALL training examples
        /// </summary>
        [Parameter("The number of iterations over ALL training examples", DefaultValue = 1, MinValue = 1)]
        public int Epochs { get; set; }

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

        private readonly List<Example> mistakes = new List<Example>();
        private readonly List<double> perceptronWeights = new List<double>();
        private double confidenceRange;
        private IArray<double> currentPerceptron;

        private void DoTrainingStep(Example binaryExample, double weight)
        {
            // if current classifies correctly, increase the weight of
            // the current perceptron
            if (Math.Sign(this.Kernel.Evaluate(this.currentPerceptron, binaryExample.Features)) == binaryExample.Class)
                this.perceptronWeights[this.perceptronWeights.Count - 1] += weight;
            // create a new perceptron
            else
            {
                // save the mistake
                this.mistakes.Add(binaryExample);

                // add a new weight
                this.perceptronWeights.Add(weight);

                // Update the current perceptron: v_k+1 = v_k + y_i * x_i
                for (int a = 0; a < binaryExample.Features.Count; a++)
                    this.currentPerceptron[a] += binaryExample.Class * binaryExample.Features[a];
            }

            this.confidenceRange += weight;
        }

        /// <summary>
        /// Trains the classifier in an online manner
        /// </summary>
        protected override void TrainMoreBinary(IArrayView<Example> binaryExamples)
        {
            foreach (var example in binaryExamples)
                this.DoTrainingStep(example, 1.0);
        }

        /// <summary>
        /// Trains the classifier in an online manner
        /// </summary>
        protected override void TrainBinary(IArrayView<Example> binaryExamples)
        {
            this.TrainBinary(binaryExamples, (1.0 / binaryExamples.Count).NCopies(binaryExamples.Count));
        }

        /// <summary>
        /// Computes the classifier's binary prediction
        /// </summary>
        protected override int PredictBinary(Example binaryExample, out double confidence)
        {
            double innerProductSum = 0,
                weightedSum = this.perceptronWeights[0] * this.Kernel.Evaluate(innerProductSum);

            /*
             * Use the recurrence (v_i+1 . ex) = (v_i . ex) + y_i (x_i . ex) to
             * compute the remaining predictions.
             */
            for (int i = 1; i < this.mistakes.Count; i++)
            {
                innerProductSum += this.mistakes[i].Class
                        * this.mistakes[i].Features.InnerProduct(binaryExample.Features);
                weightedSum += this.perceptronWeights[i] * this.Kernel.Evaluate(innerProductSum);
            }

            // the confidence represents where we are in [-cr, cr]
            confidence = (Math.Abs(weightedSum) + this.confidenceRange) / (2 * this.confidenceRange);
            
            // rare corner case: just return the most common mistake
            if (weightedSum == 0.0)
            {
                int negCount = this.mistakes.Count(e => e.Class == this.NegativeExampleValue),
                    posCount = this.mistakes.Count(e => e.Class == this.PositiveExampleValue);
                return negCount > posCount ? this.NegativeExampleValue : this.PositiveExampleValue;
            }

            return Math.Sign(weightedSum);
        }

        /// <summary>
        /// Trains the classifier
        /// </summary>
        public void Train(IArrayView<Example> labeledExamples, IArrayView<double> weights)
        {
            this.TrainBinary(labeledExamples.SelectArray(this.ConvertToBinaryExample), weights);
            this.IsTrained = true;
        }

        /// <summary>
        /// Trains the classifier
        /// </summary>
        private void TrainBinary(IArrayView<Example> binaryExamples, IArrayView<double> weights)
        {
            this.mistakes.Clear();
            this.perceptronWeights.Clear();
            this.confidenceRange = 0;

            // We need padding, since the first weight refers to the zeros vector
            this.mistakes.Add(default(Example));
            this.perceptronWeights.Add(0);
            this.currentPerceptron = Arrays.New<double>(binaryExamples[0].Features.Count);

            // in each epoch
            for (int i = 0; i < this.Epochs; i++)
                // train on each example
                for (int e = 0; e < binaryExamples.Count; e++)
                    this.DoTrainingStep(binaryExamples[e], weights[e]);
        }
    }
}
