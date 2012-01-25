using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.GUI.Configurations;
using MCAEmotiv.Classification;
using MCAEmotiv.Interop;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// A class which manages the training of a classifier with a classification scheme
    /// </summary>
    [Serializable]
    public class ClassifierManager
    {
        [Serializable]
        private class Result
        {
            public int NumActuals { get; set; }
            public int NumPredicted { get; set; }
            public int NumCorrect { get; set; }
            public double TotalConfidence { get; set; }

            public double Accuracy { get { return this.NumCorrect / this.NumActuals.ToDouble(); } }
            public double Confidence { get { return this.TotalConfidence / this.NumPredicted; } }

            public Result()
            {
                this.NumActuals = this.NumPredicted = this.NumCorrect = 0;
                this.TotalConfidence = 0;
            }
        }

        /// <summary>
        /// The settings for the classifier
        /// </summary>
        public GeneralClassifierSettings Settings { get; private set; }

        /// <summary>
        /// An online version of the underlying classifier
        /// </summary>
        public IOnlineClassifier Classifier { get; private set; }
        private readonly List<IArrayView<EEGDataEntry>> queuedTrials = new List<IArrayView<EEGDataEntry>>();
        private readonly Dictionary<int, Result> results = new Dictionary<int, Result>();
        private int minFeatures;
        private IArrayView<int> selectedBins;
        private IArrayView<double> means, stddevs;

        /// <summary>
        /// Returns the accuracy of the classifier so far
        /// </summary>
        public double Accuracy
        {
            get 
            { 
                var correct = results.Values.Select(r => r.NumCorrect).Sum();
                return correct == 0 ? 0 : correct / results.Values.Select(r => r.NumActuals).Sum().ToDouble();
            }
        }

        /// <summary>
        /// Returns the total confidence of the classifier so far
        /// </summary>
        public double Confidence
        {
            get 
            { 
                var conf = results.Values.Select(r => r.TotalConfidence).Sum();
                return conf == 0 ? 0 : results.Values.Select(r => r.NumPredicted).Sum(); 
            }
        }

        /// <summary>
        /// Constructs a manager for the given scheme
        /// </summary>
        public ClassifierManager(ClassificationScheme classificationScheme)
        {
            this.Settings = classificationScheme.Settings;
            this.Classifier = classificationScheme.Classifier.GetFactory()().AsOnlineClassifier();
        }

        /// <summary>
        /// Enqueues a trial
        /// </summary>
        public void AddTrial(IArrayView<EEGDataEntry> trial)
        {
            if (trial.Count <= 0)
                throw new Exception("At least one entry required");

            this.queuedTrials.Add(trial);
        }

        /// <summary>
        /// Removes the trial if it was enqueued but not yet processed
        /// </summary>
        public void RemoveTrial(IArrayView<EEGDataEntry> trial)
        {
            this.queuedTrials.Remove(trial);
        }

        /// <summary>
        /// Uses the classifier to predict the class of the trial
        /// </summary>
        public int Predict(IArrayView<EEGDataEntry> trial, out double confidence)
        {
            var example = this.GetExample(trial);

            return this.Classifier.Predict(this.Settings.ZScoreFeatures ? example.ZScored(this.means, this.stddevs) : example, out confidence);
        }

        /// <summary>
        /// Registers a prediction result to keep track of running accuracy
        /// </summary>
        public void RecordResult(int actual, int predicted, double confidence)
        {
            var actualResult = this.results.ContainsKey(actual)
                ? this.results[actual]
                : this.results[actual] = new Result();

            actualResult.NumActuals++;
            if (predicted == actual)
            {
                actualResult.NumCorrect++;
                actualResult.NumPredicted++;
                actualResult.TotalConfidence += confidence;
            }
            else
            {
                var predictedResult = this.results.ContainsKey(predicted)
                    ? this.results[predicted]
                    : this.results[predicted] = new Result();
                predictedResult.NumPredicted++;
                predictedResult.TotalConfidence += confidence;
            }
        }

        /// <summary>
        /// Trains the classifier on all enqueued examples
        /// </summary>
        public void Train()
        {
            IArrayView<Example> examples;
            if (!this.Classifier.IsTrained)
            {
                this.minFeatures = this.queuedTrials.Select(t => t.Count).Min();
                this.selectedBins = this.Settings.SelectedBins.Where(i => i < this.minFeatures).ToIArray();

                examples = this.queuedTrials.Select(this.GetExample).ToIArray();
                if (this.Settings.ZScoreFeatures)
                    examples = examples.ZScored(out this.means, out this.stddevs);
            }
            else
            {
                examples = this.queuedTrials.Select(this.GetExample).ToIArray();
                if (this.Settings.ZScoreFeatures)
                    examples = examples.Select(e => e.ZScored(this.means, this.stddevs)).ToIArray();
            }

            // train
            if (this.Classifier.IsTrained)
                this.Classifier.TrainMore(examples);
            else
                this.Classifier.Train(examples);


            // don't waste memory!
            this.queuedTrials.Clear();
        }

        private Example GetExample(IArrayView<EEGDataEntry> trial)
        {
            int marker = trial.FirstItem().Marker;

            var means = Channels.Values.Select(ch => trial.Channel(ch).Average()).ToArray();

            IArrayView<EEGDataEntry> trimmedTrial = trial.DownSample(this.Settings.BinWidthMillis);
            if (trial.Count > this.minFeatures)
                trimmedTrial = trial.SubView(0, this.minFeatures);
            else if (trial.Count < this.minFeatures)
            {
                var defaultEntry = new EEGDataEntry(marker, 0, 0, means);
                trimmedTrial = Arrays.FromMap(i => i < trial.Count ? trial[i] : defaultEntry, this.minFeatures);
            }

            var features = trimmedTrial
                .Select(this.selectedBins)
                .SelectMany(e => this.Settings.SelectedChannels.Select(ch => e[ch] - means[ch.ToIndex()]));
            if (this.Settings.IncludeChannelMeans)
                features = features.Concat(this.Settings.SelectedChannels.Select(ch => means[ch.ToIndex()]));

            return new Example(marker, features);
        }
    }
}
