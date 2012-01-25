using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv;
using MCAEmotiv.Classification;
using System.IO;

namespace MCAEmotiv.Testing.Unit
{
    static class ClassifierTests
    {
        public static void Run()
        {
            foreach (var classifier in GetClassifiers())
            {
                try
                {
                    classifier.TestSerializable(Parameters.HasEqualParameters);
                    classifier.TestTraining();
                    if (classifier is IOnlineClassifier)
                        (classifier as IOnlineClassifier).TestOnlineTraining();
                }
                catch (Exception e)
                {
                    (classifier.GetType().Name + ": ").Print();
                    e.Print();
                }
            }
        }

        public static IEnumerable<IClassifier> GetClassifiers()
        {
            yield return new KNN();
            yield return new PenalizedLogisticRegression();
            yield return new PenalizedLogisticRegression().AsOnlineClassifier();
            yield return new VotedPerceptron();
            yield return new DecisionStump();
            yield return new AdaBoost();
        }

        public static IArrayView<Example> GetExamples(int numExamples = 300, int numAttributes = 10, double noise = 0.3)
        {
            var rand = new Random();
            var means = numAttributes
                .CountTo()
                .Select(i => Tuples.New(rand.NextDouble(), rand.NextDouble()))
                .ToIArray();
            return numExamples
                .CountTo()
                .Select(i => (i % 2) == 0 ? 3 : 4)
                .Select(cls =>
                {
                    return new Example(cls, means
                        .Select(d => (cls == 3 ? d.Item1 : d.Item2) + (2 * noise * rand.NextDouble()) - noise));
                })
                .ToIArray();
        }

        public static void TestTraining(this IClassifier c)
        {
            if (c.IsTrained)
                throw new Exception("Already trained");

            // IsTrained test
            var examples = GetExamples();
            var test = examples.SubView(0, (.1 * examples.Count).Rounded());
            var train = examples.SubView(test.Count, examples.Count - test.Count);
            c.Train(train);
            if (!c.IsTrained)
                throw new Exception("Classifier is trained");

            // accuracy test
            double minAccuracy = 0.6;
            if (c.AccuracyOn(train) < minAccuracy && !(c.GetType() == typeof(DecisionStump) && c.AccuracyOn(train) > 0.5))
                throw new Exception("Training accuracy < " + minAccuracy);
            if (c.AccuracyOn(test) < minAccuracy && c.GetType() != typeof(DecisionStump))
                throw new Exception("Test accuray < " + minAccuracy);
        }

        public static void TestOnlineTraining(this IOnlineClassifier c)
        {
            // train on an initial set of random examples
            c.Train(GetExamples(10));

            // now try a new set with no correlation to the previous one
            var examples = GetExamples();
            var test = examples.SubView(0, (.1 * examples.Count).Rounded());
            var train = examples.SubView(test.Count, examples.Count - test.Count);
            c.TrainMore(train);

            // accuracy test
            double minAccuracy = 0.6;
            if (c.AccuracyOn(train) < minAccuracy)
                throw new Exception("Training accuracy < " + minAccuracy);
            if (c.AccuracyOn(test) < minAccuracy)
                throw new Exception("Test accuray < " + minAccuracy);
        }
    }
}
