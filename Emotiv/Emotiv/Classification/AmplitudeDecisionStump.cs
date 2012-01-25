using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.Classification
{
    /// <summary>
    /// A version of decision stump that trains on the average absolute value of the supplied time series. This is useful for eyes open/eyes closed experiments
    /// </summary>
    [Serializable]
    [Description("A version of decision stump that trains on the average absolute value of the supplied time series. This is useful for eyes open/eyes closed experiments", 
        DisplayName = "Average Amplitude Decision Stump")]
    public class AmplitudeDecisionStump : AbstractBinaryClassifier
    {
        private readonly DecisionStump stump = new DecisionStump();

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
            get { return 1; }
        }

        /// <summary>
        /// Trains the classifier
        /// </summary>
        protected override void TrainBinary(IArrayView<Example> binaryExamples)
        {
            this.stump.Train(binaryExamples.Select(ConvertToAverageExample).ToIArray());
        }

        /// <summary>
        /// Classifies the example
        /// </summary>
        protected override int PredictBinary(Example binaryExample, out double confidence)
        {
            return this.stump.Predict(ConvertToAverageExample(binaryExample), out confidence);
        }

        private static Example ConvertToAverageExample(Example example)
        {
            return new Example(example.Class, example.Features.Select(Math.Abs).Average().Enumerate());
        }

        /// <summary>
        /// Returns false
        /// </summary>
        public override bool ComputesConfidence
        {
            get { return false; }
        }
    }
}
