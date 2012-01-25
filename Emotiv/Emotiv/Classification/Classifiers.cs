using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.Classification
{
    #region ---- Interfaces ----
    /// <summary>
    /// A basic interface which all classifiers must implement.
    /// </summary>
    [Description("A supervised machine-learning classification algorithm", DisplayName = "Classifier")]
    public interface IClassifier
    {
        /// <summary>
        /// Has the classifier been trained?
        /// </summary>
        bool IsTrained { get; }

        /// <summary>
        /// Should the classifier's confidence output be treated as valid?
        /// </summary>
        bool ComputesConfidence { get; }

        /// <summary>
        /// Train the classifier on the provided labeled examples.
        /// </summary>
        void Train(IArrayView<Example> labeledExamples);
        
        /// <summary>
        /// Predict the class of example. If example is labeled, the label will be ignored. 
        /// </summary>
        /// <param name="example">the example to be classified</param>
        /// <param name="confidence">An out parameter whose value may reflect the classifier's confidence in its prediciton</param>
        /// <returns>The predicted class of the example</returns>
        int Predict(Example example, out double confidence);
    }

    /// <summary>
    /// A classifier that supports only two classes
    /// </summary>
    public interface IBinaryClassifier : IClassifier
    {
        /// <summary>
        /// Returns true if cls maps to the positive binary class, false if cls maps
        /// to the negative binary class, and null otherwise.
        /// </summary>
        bool? GetBinaryClass(int cls);
    }

    /// <summary>
    /// A classifier supporting weighted examples
    /// </summary>
    public interface IWeightedClassifier : IClassifier
    {
        /// <summary>
        /// Trains the classifier, weighting each parameter by the corresponding weight value in weights.
        /// </summary>
        void Train(IArrayView<Example> labeledExamples, IArrayView<double> weights);
    }

    /// <summary>
    /// A classifier that can be trained in an online fashion
    /// </summary>
    public interface IOnlineClassifier : IClassifier
    {
        /// <summary>
        /// Rather than completely re-training the classifier, gives the classifier additional
        /// training based on labeledExamples.
        /// </summary>
        void TrainMore(IArrayView<Example> labeledExamples);
    }
    #endregion

    #region ---- Abstract Classes ----
    /// <summary>
    /// An abstract base class for classifiers.
    /// </summary>
    [Serializable]
    public abstract class AbstractClassifier : IClassifier
    {
        /// <summary>
        /// Has the classifier been trained?
        /// </summary>
        public bool IsTrained { get; protected set; }

        /// <summary>
        /// Does the classifier compute a valid confidence value?
        /// </summary>
        public abstract bool ComputesConfidence { get; }

        /// <summary>
        /// Construct an abstract classifier
        /// </summary>
        protected AbstractClassifier()
        {
            this.IsTrained = false;
            this.SetParametersToDefaultValues();
        }

        /// <summary>
        /// Checks that the classifiers parameters are valid and calls TrainHelper. Sets IsTrained to true.
        /// </summary>
        public void Train(IArrayView<Example> labeledExamples)
        {
            string errorMessage;
            if (!this.AreParameterValuesValid(out errorMessage))
                throw new Exception("Invalid parameter value: " + errorMessage);

            this.TrainHelper(labeledExamples);
            this.IsTrained = true;
        }

        /// <summary>
        /// Called by Train
        /// </summary>
        protected abstract void TrainHelper(IArrayView<Example> labeledExamples);

        /// <summary>
        /// Throws an execption is the classifier has not been trained, then calls PredictHelper.
        /// </summary>
        public int Predict(Example example, out double confidence)
        {
            if (!this.IsTrained)
                throw new Exception("Classifier is not yet trained!");

            return this.PredictHelper(example, out confidence);
        }

        /// <summary>
        /// Called by Predict
        /// </summary>
        protected abstract int PredictHelper(Example example, out double confidence);

        /// <summary>
        /// Checks for identity equality or type and parameter equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj == this ||
                (obj.GetType() == this.GetType()
                && this.HasEqualParameters(obj)); 
        }

        /// <summary>
        /// Returns the hash code of this classifier's type.
        /// </summary>
        public override int GetHashCode()
        {
            return this.GetType().GetHashCode();
        }
    }

    /// <summary>
    /// A base class for binary classifiers
    /// </summary>
    [Serializable]
    public abstract class AbstractBinaryClassifier : AbstractClassifier, IBinaryClassifier
    {
        private readonly Dictionary<int, int> classMap = new Dictionary<int, int>(2);

        /// <summary>
        /// The classifier's preferred class value for negative examples
        /// </summary>
        public abstract int NegativeExampleValue { get; }
        
        /// <summary>
        /// The classifier's preferred class value for positive examples
        /// </summary>
        public abstract int PositiveExampleValue { get; }

        /// <summary>
        /// Converts the examples to have binary class values, then calls TrainBinary. The mappings from
        /// regular classes to binary classes are stored until future calls to TrainHelper.
        /// </summary>
        protected override void TrainHelper(IArrayView<Example> examples)
        {
            var classes = examples.Select(e => e.Class).Distinct().InOrder().ToIArray();
            if (classes.Count > 2)
                throw new ArgumentException("Training set has too many classes for binary classifier");
            this.classMap.Clear();
            this.classMap[classes.FirstItem()] = this.NegativeExampleValue;
            this.classMap[classes.LastItem()] = this.PositiveExampleValue;

            this.TrainBinary(examples.SelectArray(this.ConvertToBinaryExample));
        }

        /// <summary>
        /// Returns an example whose class is the binary equivalent of the given example's class and
        /// whose features are the same as the given example's features
        /// </summary>
        protected Example ConvertToBinaryExample(Example nonBinaryExample)
        {
            if (!this.classMap.ContainsKey(nonBinaryExample.Class))
            {
                switch (this.classMap.Count)
                {
                    case 0:
                        this.classMap[nonBinaryExample.Class] = this.NegativeExampleValue;
                        break;
                    case 1:
                        this.classMap[nonBinaryExample.Class] = this.classMap.Values.First() == this.NegativeExampleValue
                            ? this.PositiveExampleValue
                            : this.NegativeExampleValue;
                        break;
                    default:
                        throw new Exception("Class " + nonBinaryExample.Class + " not found!");
                }
            }

            return nonBinaryExample.WithClass(this.classMap[nonBinaryExample.Class]);
        }

        /// <summary>
        /// Returns true if cls maps to a positive example, false if cls maps to a negative
        /// example, and null if there is no mapping for cls.
        /// </summary>
        public bool? GetBinaryClass(int cls)
        {
            return this.classMap.ContainsKey(cls)
                ? this.classMap[cls] == this.PositiveExampleValue
                : (bool?)null;
        }

        /// <summary>
        /// The values of the pairs are the labels mapped to either NegativeExampleValue or PositiveExampleValue.
        /// </summary>
        protected abstract void TrainBinary(IArrayView<Example> binaryExamples);

        /// <summary>
        /// Calls PredictBinary, and then converts the prediction back to a regular class value.
        /// </summary>
        protected override int PredictHelper(Example example, out double confidence)
        {
            int binaryClass = this.PredictBinary(example, out confidence);

            return this.classMap.Where(p => p.Value == binaryClass).First().Key;
        }

        /// <summary>
        /// Classify as NegativeExampleValue or PositiveExampleValue
        /// </summary>
        protected abstract int PredictBinary(Example binaryExample, out double confidence);
    }

    /// <summary>
    /// A base class for an online binary classifier
    /// </summary>
    [Serializable]
    public abstract class AbstractOnlineBinaryClassifier : AbstractBinaryClassifier, IOnlineClassifier
    {
        /// <summary>
        /// Converts the examples to binary and then calls TrainMoreBinary.
        /// </summary>
        public void TrainMore(IArrayView<Example> labeledExamples)
        {
            if (this.IsTrained)
                this.TrainMoreBinary(labeledExamples.SelectArray(this.ConvertToBinaryExample));
            else
                this.Train(labeledExamples);
        }

        /// <summary>
        /// Called by TrainMore if the classifier is already trained (otherwise Train is called).
        /// </summary>
        protected abstract void TrainMoreBinary(IArrayView<Example> binaryExamples);
    }
    #endregion

    #region ---- Extensions ----
    /// <summary>
    /// Extension methods for Classifiers
    /// </summary>
    public static class Classifiers
    {
        /// <summary>
        /// Predict without the confidence parameter
        /// </summary>
        public static int Predict(this IClassifier classifier, Example example)
        {
            double ignored;
            return classifier.Predict(example, out ignored);
        }

        /// <summary>
        /// Returns a Trio of (example, predicted class, confidence) for each example 
        /// </summary>
        public static IEnumerable<Trio<Example, int, double>> Predict(this IClassifier classifier, IEnumerable<Example> examples)
        {
            int cls;
            double confidence;
            foreach (var example in examples)
            {
                cls = classifier.Predict(example, out confidence);
                yield return Tuples.New(example, cls, confidence);
            }
        }

        /// <summary>
        /// Computes the classifier's accuracy in predicting the set of examples
        /// </summary>
        public static double AccuracyOn(this IClassifier classifier, IEnumerable<Example> labeledExamples)
        {
            int rightCount = 0;
            foreach (var example in labeledExamples)
                if (classifier.Predict(example) == example.Class)
                    rightCount++;

            return rightCount / labeledExamples.Count().ToDouble();
        }

        /// <summary>
        /// Computes the classifier's error in predicting the set of examples
        /// </summary>
        public static double ErrorOn(this IClassifier classifier, IEnumerable<Example> labeledExamples)
        {
            return 1.0 - classifier.AccuracyOn(labeledExamples);
        }

        /// <summary>
        /// Does cls currently map to a positive example in this classifier?
        /// </summary>
        public static bool IsPositive(this IBinaryClassifier classifier, int cls)
        {
            bool? isPositive = classifier.GetBinaryClass(cls);
            if (isPositive.HasValue)
                return isPositive.Value;

            throw new Exception("Classifier does not contain a mapping for cls");
        }

        /// <summary>
        /// Does cls currently map to a negative example in this classifier?
        /// </summary>
        public static bool IsNegative(this IBinaryClassifier classifier, int cls)
        {
            bool? isPositive = classifier.GetBinaryClass(cls);
            if (isPositive.HasValue)
                return !isPositive.Value;

            throw new Exception("Classifier does not contain a mapping for cls");
        }

        /// <summary>
        /// As <code>classifier.IsPositive(classifier, example.Class)</code>
        /// </summary>
        public static bool IsPositive(this IBinaryClassifier classifier, Example example)
        {
            return classifier.IsPositive(example.Class);
        }
        
        /// <summary>
        /// As <code>classifier.IsNegative(classifier, example.Class)</code>
        /// </summary>
        public static bool IsNegative(this IBinaryClassifier classifier, Example example)
        {
            return classifier.IsNegative(example.Class);
        }

        /// <summary>
        /// TrainMore, but with a single example
        /// </summary>
        public static void TrainMore(this IOnlineClassifier classifier, Example labeledExample)
        {
            classifier.TrainMore(labeledExample.NCopies(1));
        }

        /// <summary>
        /// Wraps the classifier in an online classifier which works by storing all input examples
        /// and retraining the classifier on the stored examples as well as the new examples on each
        /// call to TrainMore
        /// </summary>
        public static IOnlineClassifier AsOnlineClassifier(this IClassifier classifier)
        {
            return classifier as IOnlineClassifier ?? new OnlineClassifierAdapter(classifier);
        }

        #region ---- Implementations ----
        [Serializable]
        private class OnlineClassifierAdapter : AbstractClassifier, IOnlineClassifier
        {
            private readonly IClassifier classifier;
            private readonly List<Example> examples = new List<Example>();

            public override bool ComputesConfidence { get { return this.classifier.ComputesConfidence; } }

            public OnlineClassifierAdapter(IClassifier classifier)
                : base()
            {
                this.classifier = classifier;
            }

            protected override void TrainHelper(IArrayView<Example> labeledExamples)
            {
                this.examples.Clear();
                this.examples.AddRange(labeledExamples);
                this.classifier.Train(labeledExamples);
            }

            protected override int PredictHelper(Example example, out double confidence)
            {
                return this.classifier.Predict(example, out confidence);
            }

            public void TrainMore(IArrayView<Example> labeledExamples)
            {
                this.examples.AddRange(labeledExamples);
                this.classifier.Train(this.examples.AsIArray());
            }
        }
        #endregion
    }
    #endregion
}
