using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.Interop;
using MCAEmotiv.Classification;

namespace MCAEmotiv.GUI.Configurations
{
    /// <summary>
    /// Contains settings for determining which channels should be used in feature selection.
    /// </summary>
    [Serializable]
    [Description(DESCRIPTION, DisplayName = DISPLAY_NAME)]
    public class ChannelSelectionSettings
    {
        /// <summary>
        /// The description and display name of this class
        /// </summary>
        public const string DESCRIPTION = "EEG channel selection", DISPLAY_NAME = "Channel Selection";

        #region ---- Channel Parameters ----
        /// <summary>
        /// Should channel AF3 be used?
        /// </summary>
        [Parameter("Channel AF3", DisplayName = "AF3", DefaultValue = true)]
        public bool UseAF3 { get; set; }

        /// <summary>
        /// Should channel F7 be used?
        /// </summary>
        [Parameter("Channel F7", DisplayName = "F7", DefaultValue = true)]
        public bool UseF7 { get; set; }

        /// <summary>
        /// Should channel F3 be used?
        /// </summary>
        [Parameter("Channel F3", DisplayName = "F3", DefaultValue = true)]
        public bool UseF3 { get; set; }

        /// <summary>
        /// Should channel FC5 be used?
        /// </summary>
        [Parameter("Channel FC5", DisplayName = "FC5", DefaultValue = true)]
        public bool UseFC5 { get; set; }

        /// <summary>
        /// Should channel T7 be used?
        /// </summary>
        [Parameter("Channel T7", DisplayName = "T7", DefaultValue = true)]
        public bool UseT7 { get; set; }

        /// <summary>
        /// Should channel P7 be used?
        /// </summary>
        [Parameter("Channel P7", DisplayName = "P7", DefaultValue = true)]
        public bool UseP7 { get; set; }

        /// <summary>
        /// Should channel O1 be used?
        /// </summary>
        [Parameter("Channel O1", DisplayName = "O1", DefaultValue = true)]
        public bool UseO1 { get; set; }

        /// <summary>
        /// Should channel O2 be used?
        /// </summary>
        [Parameter("Channel O2", DisplayName = "O2", DefaultValue = true)]
        public bool UseO2 { get; set; }
        
        /// <summary>
        /// Should channel P8 be used?
        /// </summary>
        [Parameter("Channel P8", DisplayName = "P8", DefaultValue = true)]
        public bool UseP8 { get; set; }

        /// <summary>
        /// Should channel T8 be used?
        /// </summary>
        [Parameter("Channel T8", DisplayName = "T8", DefaultValue = true)]
        public bool UseT8 { get; set; }

        /// <summary>
        /// Should channel FC6 be used?
        /// </summary>
        [Parameter("Channel FC6", DisplayName = "FC6", DefaultValue = true)]
        public bool UseFC6 { get; set; }

        /// <summary>
        /// Should channel F4 be used?
        /// </summary>
        [Parameter("Channel F4", DisplayName = "F4", DefaultValue = true)]
        public bool UseF4 { get; set; }

        /// <summary>
        /// Should channel F8 be used?
        /// </summary>
        [Parameter("Channel F8", DisplayName = "F8", DefaultValue = true)]
        public bool UseF8 { get; set; }

        /// <summary>
        /// Should channel AF4 be used?
        /// </summary>
        [Parameter("Channel AF4", DisplayName = "AF4", DefaultValue = true)]
        public bool UseAF4 { get; set; }
        #endregion

        /// <summary>
        /// The set of selected channels
        /// </summary>
        public IEnumerable<Channel> Channels
        {
            get
            {
                foreach (var parameter in this.GetParameters())
                    if ((bool)this.GetProperty(parameter.Property))
                        yield return ChannelFromParameter(parameter);
            }
            set
            {
                var set = new HashSet<Channel>(value);
                foreach (var parameter in this.GetParameters())
                    this.SetProperty(parameter.Property, set.Contains(ChannelFromParameter(parameter)));
            }
        }

        /// <summary>
        /// Construct a new settings object with default values
        /// </summary>
        public ChannelSelectionSettings() { this.SetParametersToDefaultValues(); }

        private static Channel ChannelFromParameter(ParameterAttribute parameter)
        {
            return (Channel)Enum.Parse(typeof(Channel), parameter.DisplayName);
        }
    }

    /// <summary>
    /// Contains configuration information for an online classifier
    /// </summary>
    [Serializable]
    [Description("General classifier configuration", DisplayName = "General Settings")]
    public class GeneralClassifierSettings
    {
        /// <summary>
        /// MAX_TIME is the maximum length trial supported by the classifier.
        /// MAX_BINS is the maximum bin count supported by the classifier
        /// </summary>
        public const int MAX_BINS = 800, MAX_TIME = 10000;

        /// <summary>
        /// A name used to identify this classifier
        /// </summary>
        [Parameter("A name used to identify this classifier", DefaultValue = "")]
        public string Name { get; set; }

        /// <summary>
        /// The width (in ms) of the time bins after downsampling
        /// </summary>
        [Parameter("The width (in ms) of the time bins after downsampling", DisplayName = "Time Bin Width", DefaultValue = 20, MinValue = 1)]
        public int BinWidthMillis { get; set; }

        /// <summary>
        /// Should the voltage along each selected channel be included as a feature?
        /// </summary>
        [Parameter("Should the voltage along each selected channel be included as a feature?", DisplayName = "Channel Means", DefaultValue = true)]
        public bool IncludeChannelMeans { get; set; }

        /// <summary>
        /// Should each feature be normalized to have mean 0 and standard deviation 1?
        /// </summary>
        [Parameter("Should each feature be normalized to have mean 0 and standard deviation 1?", DisplayName = "Z-score", DefaultValue = false)]
        public bool ZScoreFeatures { get; set; }

        /// <summary>
        /// EEG channel selection
        /// </summary>
        [Parameter(ChannelSelectionSettings.DESCRIPTION, DisplayName = ChannelSelectionSettings.DISPLAY_NAME, DefaultValue = typeof(ChannelSelectionSettings))]
        public ChannelSelectionSettings ChannelSettings { get; set; }

        /// <summary>
        /// The time bins to be used for this classifier. Not a parameter
        /// </summary>
        [Description("The time bins to be used for this classifier", DisplayName = "Selected Time Bins")]
        public IArrayView<int> SelectedBins { get; set; }

        /// <summary>
        /// The channels selected to be used by the classifier. Not a parameter
        /// </summary>
        public IArrayView<Channel> SelectedChannels { get { return this.ChannelSettings.Channels.ToIArray(); } }

        /// <summary>
        /// The number of time bins used by this classifier. Not a parameter
        /// </summary>
        public int BinCount { get { return GetBinCount(this.BinWidthMillis); } }

        /// <summary>
        /// The total number of features used by this classifier. Not a parameter
        /// </summary>
        public int FeatureCount { get { return this.SelectedBins.Count * this.SelectedChannels.Count; } }

        /// <summary>
        /// Construct a settings object with default values
        /// </summary>
        public GeneralClassifierSettings()
        {
            this.SetParametersToDefaultValues();
            this.SelectedBins = GetBinCount(this.BinWidthMillis).CountTo();
        }

        /// <summary>
        /// Returns the number of bins for the given bin width
        /// </summary>
        public static int GetBinCount(int binWidthMillis)
        {
            return Math.Min(MAX_BINS, Math.Max(MAX_TIME / binWidthMillis, 1));
        }
    }

    /// <summary>
    /// Represents a scheme for classification, consisting of both a classifier and a feature selection scheme
    /// </summary>
    [Serializable]
    [Description("A specification of a classifier and preprocessing/feature selection steps", DisplayName = "Classification Scheme")]
    public class ClassificationScheme
    {
        /// <summary>
        /// The extension used when saving classification schemes
        /// </summary>
        public const string EXTENSION = ".classifier";

        /// <summary>
        /// The classifier for this scheme
        /// </summary>
        [Parameter("The classifier used by the scheme", DefaultValue = typeof(VotedPerceptron))]
        public IClassifier Classifier { get; set; }

        /// <summary>
        /// The preprocessing and feature selection settings used by the scheme
        /// </summary>
        [Parameter("The preprocessing and feature selection settings used with this scheme", DefaultValue = typeof(GeneralClassifierSettings))]
        public GeneralClassifierSettings Settings { get; set; }

        /// <summary>
        /// Construct a new classification scheme with default parameters
        /// </summary>
        public ClassificationScheme() { this.SetParametersToDefaultValues(); }

        /// <summary>
        /// Returns a loggable string representation of the scheme
        /// </summary>
        public override string ToString()
        {
            return (this.Settings.Name + " {")
                .Then(("Classifier" + this.Classifier.PrettyPrint(true))
                    .Then("Settings {")
                    .Then(this.Settings.PrettyPrint().Indent())
                    .Then("}")
                    .Then("Selected Time Bins {")
                    .Concat(this.Settings.SelectedBins
                        .Select(i => string.Format("\t{0}-{1}ms", i * this.Settings.BinWidthMillis, (i + 1) * this.Settings.BinWidthMillis)))
                    .Then("}")
                    .ConcatToString(Environment.NewLine)
                    .Indent())
                .Then("}")
                .ConcatToString(Environment.NewLine);
        }
    }
}
