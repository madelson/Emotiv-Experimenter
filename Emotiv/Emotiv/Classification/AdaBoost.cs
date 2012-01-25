using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.Interop;

namespace MCAEmotiv.Classification
{
    /// <summary>
    /// Determines how a subset of features is selected to train each weak learner
    /// </summary>
    [Description("Determines how a subset of features is selected to train each weak learner", DisplayName = "Weak Learner Training Mode")]
    public enum WeakLearnerTrainingModes
    {
        /// <summary>
        /// Each weak learner is trained on all features
        /// </summary>
        [Description("Each weak learner is trained on all features", DisplayName = "All Features")]
        AllFeatures,

        /// <summary>
        /// Each weak learner is trained on a single random feature
        /// </summary>
        [Description("Each weak learner is trained on a single random feature", DisplayName = "Random Feature")]
        RandomFeature,

        /// <summary>
        /// Each weak learner is trained on a different feature
        /// </summary>
        [Description("Each weak learner is trained on a different feature", DisplayName = "Sequential Feature")]
        SequentialFeature,

        /// <summary>
        /// Each weak learner is trained on a random subset of features
        /// </summary>
        [Description("Each weak learner is trained on a random subset of features", DisplayName = "Random Subset")]
        RandomSubset,

        /// <summary>
        /// Each weak learner is trained on the features in a random time bin
        /// </summary>
        [Description("Each weak learner is trained on the features in a random time bin", DisplayName = "Random Time Bin")]
        RandomTimeBin,

        /// <summary>
        /// Each weak learner is trained on a different time bin's features
        /// </summary>
        [Description("Each weak learner is trained on a different time bin's features", DisplayName = "Sequential Time Bin")]
        SequentialTimeBin,
    }

    /// <summary>
    /// A classifier based on the AdaBoost ensemble learning algorithm.
    /// </summary>
    [Serializable]
    [Description("A classifier which combines the results of an ensemble of \"weak learners\" which are themselves other classifiers")]
    public class AdaBoost : AbstractBinaryClassifier
    {
        /// <summary>
        /// The number of rounds of boosting to perform
        /// </summary>
        [Parameter("The number of rounds of boosting to perform", DefaultValue = 10, MinValue = 1)]
        public int Rounds { get; set; }

        /// <summary>
        /// Determines how a subset of features is selected to train the weak learner
        /// </summary>
        [Parameter("Determines how a subset of features is selected to train the weak learner", DisplayName = "Weak Learner Training Mode", DefaultValue = WeakLearnerTrainingModes.RandomSubset)]
        public WeakLearnerTrainingModes WeakLearnerTrainingMode { get; set; }

        /// <summary>
        /// The type of weak learner used by AdaBoost
        /// </summary>
        [Parameter("The type of weak learner used by AdaBoost", DisplayName = "Weak Learner", DefaultValue = typeof(DecisionStump))]
        public IWeightedClassifier WeakLearnerTemplate { get; set; }

        private Random rand;
        private IWeightedClassifier[] weakLearners;
        private IArrayView<int>[] weakLearnerFeatures;
        private double[] alphas;

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
        /// Predicts the binary class of the binary example using the weighted vote of each weak learner
        /// </summary>
        protected override int PredictBinary(Example binaryExample, out double confidence)
        {
            confidence = 1.0;
            double weightedVote = 0;
            for (int i = 0; i < this.weakLearners.Length; i++)
                weightedVote += this.alphas[i] * 
                    this.weakLearners[i].Predict(binaryExample.WithFeatures(this.weakLearnerFeatures[i]));

            if (weightedVote == 0)
                return this.NegativeExampleValue;
            return Math.Sign(weightedVote);
        }

        /// <summary>
        /// Returns false
        /// </summary>
        public override bool ComputesConfidence
        {
            get { return false; }
        }

        /// <summary>
        /// Trains the classifier on the supplied binary examples
        /// </summary>
        protected override void TrainBinary(IArrayView<Example> binaryExamples)
        {
            this.rand = new Random(unchecked(this.Rounds * binaryExamples.Count * binaryExamples[0].Features.Count));
            this.weakLearners = new IWeightedClassifier[this.Rounds];
            this.weakLearnerFeatures = new IArrayView<int>[this.Rounds];
            this.alphas = new double[this.Rounds];
            var predictionResults = new bool[binaryExamples.Count];
            var weights = (1.0 / binaryExamples.Count).NCopies(binaryExamples.Count).ToIArray();
            var factory = this.WeakLearnerTemplate.GetFactory();

            double trainingError;
            for (int i = 0; i < this.Rounds; i++)
            {
                this.weakLearners[i] = factory();
                trainingError = this.TrainWeakLearner(binaryExamples, weights, i, predictionResults);
                this.alphas[i] = this.GetAlpha(trainingError);

                // update weights ( w *= exp(-alpha * y_i * h(x_i)) )
                double expAlpha = Math.Exp(this.alphas[i]), normalizationFactor = 0;
                for (int e = 0; e < binaryExamples.Count; e++)
                    if (predictionResults[e])
                        normalizationFactor += (weights[e] /= expAlpha);
                    else
                        normalizationFactor += (weights[e] *= expAlpha);

                // normalize weights
                for (int e = 0; e < binaryExamples.Count; e++)
                    weights[e] /= normalizationFactor;
            }
        }

        private double GetAlpha(double trainingError)
        {
            // avoid divide-by-zero errors
            if (trainingError == 0)
                trainingError = 1e-6;

            return 0.5 * Math.Log((1 - trainingError) / trainingError);
        }

        private double TrainWeakLearner(IArrayView<Example> binaryExamples, 
            IArrayView<double> weights, 
            int round, 
            bool[] predictionResults)
        {
            this.weakLearnerFeatures[round] = this.SelectFeatures(binaryExamples, round);
            this.weakLearners[round].Train(binaryExamples.SelectArray(e => e.WithFeatures(this.weakLearnerFeatures[round])), weights);
            double trainingError = 0;
            for (int i = 0; i < binaryExamples.Count; i++)
                if (!(predictionResults[i] =
                    (this.weakLearners[round].Predict(binaryExamples[i].WithFeatures(this.weakLearnerFeatures[round])) == binaryExamples[i].Class)))
                    trainingError += weights[i];

            return trainingError;
        }

        private IArrayView<int> SelectFeatures(IArrayView<Example> binaryExamples, int round)
        {
            switch (this.WeakLearnerTrainingMode)
            {
                case WeakLearnerTrainingModes.AllFeatures:
                    return binaryExamples[0].Features.Indices();
                case WeakLearnerTrainingModes.RandomSubset:
                    return binaryExamples[0].Features
                        .Indices()
                        .Shuffled(this.rand)
                        .SubView(0, this.rand.Next(1, binaryExamples[0].Features.Count));
                case WeakLearnerTrainingModes.RandomFeature:
                    return this.rand.Next(binaryExamples[0].Features.Count).NCopies(1);
                case WeakLearnerTrainingModes.SequentialFeature:
                    return (round % binaryExamples[0].Features.Count).NCopies(1);
                case WeakLearnerTrainingModes.RandomTimeBin:
                    int bin = this.rand.Next(binaryExamples[0].Features.Count / Channels.Values.Count),
                        start = Channels.Values.Count * bin,
                        count = Channels.Values.Count;
                    if (start + count > binaryExamples[0].Features.Count)
                        count = binaryExamples[0].Features.Count - start;
                    return Arrays.Range(start, 1, count);
                case WeakLearnerTrainingModes.SequentialTimeBin:
                    return Arrays.Range((Channels.Values.Count * round) % binaryExamples[0].Features.Count, 1, Channels.Values.Count);
            }

            throw new Exception("Training mode not supported!");
        }

    }
}
