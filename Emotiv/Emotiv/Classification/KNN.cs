using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.Classification
{
    /// <summary>
    /// Classifies examples based on the classes of their k nearest neighbors in the training set
    /// </summary>
    [Serializable]
    [Description("Classifies examples based on the classes of their k nearest neighbors in the training set")]
    public class KNN : AbstractClassifier, IOnlineClassifier
    {
        private readonly List<Example> neighborhood = new List<Example>();

        /// <summary>
        /// Returns true
        /// </summary>
        public override bool ComputesConfidence { get { return true; } }

        /// <summary>
        /// The number of nearest-neighbors considered in the voting process
        /// </summary>
        [Parameter("The number of nearest-neighbors considered in the voting process", DefaultValue=1, MinValue=1)]
        public int K { get; set; }

        /// <summary>
        /// Should the algorithm weight the votes of nearest neighbors by their distance?
        /// </summary>
        [Parameter("Should the algorithm weight the votes of nearest neighbors by their distance?", DefaultValue = false)]
        public bool WeightedVoting { get; set; }

        /// <summary>
        /// Trains the classifier
        /// </summary>
        protected override void TrainHelper(IArrayView<Example> labeledExamples)
        {
            this.neighborhood.Clear();
            this.neighborhood.AddRange(labeledExamples);
        }

        /// <summary>
        /// Classifies the example
        /// </summary>
        protected override int PredictHelper(Example example, out double confidence)
        {
            if (this.neighborhood.Count == 0)
                throw new Exception("Cannot make a prediction without at least 1 training example");

            // find K nearest neighbors
            var kNearest = this.neighborhood
                .Select(e => new { Class = e.Class, Distance = e.Features.SquaredDistanceTo(example.Features) })
                .OrderBy(cd => cd.Distance)
                .Take(this.K);

            // vote
            var voteMap = new Dictionary<int, double>();
            foreach (var nearest in kNearest)
            {
                if (!voteMap.ContainsKey(nearest.Class))
                    voteMap[nearest.Class] = 0;

                voteMap[nearest.Class] += this.WeightedVoting ? (1.0 / nearest.Distance) : 1.0;
            }

            // predict
            double bestVote = double.NegativeInfinity;
            int bestClass = 0;
            foreach (var pair in voteMap)
                if (pair.Value > bestVote)
                {
                    bestVote = pair.Value;
                    bestClass = pair.Key;
                }

            confidence = bestVote / voteMap.Values.Sum();
            return bestClass;
        }

        /// <summary>
        /// Adds the examples to the set of possible neighbor examples
        /// </summary>
        public void TrainMore(IArrayView<Example> labeledExamples)
        {
            if (this.IsTrained)
                this.neighborhood.AddRange(labeledExamples);
            else
                this.Train(labeledExamples);
        }
    }
}
