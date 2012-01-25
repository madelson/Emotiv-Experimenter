using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.GUI.Configurations
{
    /// <summary>
    /// Determines how a stimulus's subclass affects the experiment
    /// </summary>
    [Serializable]
    [Description("Determines how a stimulus's subclass affects the experiment", DisplayName = "Question Mode")]
    public enum QuestionMode
    {
        /// <summary>
        /// If the user incorrectly answers the question associated with the stimulus, the trial is invalidated. Unclassified stimuli are excluded from the experiment
        /// </summary>
        [Description("If the user incorrectly answers the question associated with the stimulus, the trial is invalidated. Unclassified stimuli are excluded from the experiment",
            DisplayName = "Ask and Verify")]
        AskAndVerify,

        /// <summary>
        /// The user is required to answer the question associated with each stimulus, but the answer cannot invalidate the trial. Unclassified stimuli are included in the experiment
        /// </summary>
        [Description("The user is required to answer the question associated with each stimulus, but the answer cannot invalidate the trial. Unclassified stimuli are included in the experiment")]
        Ask,

        /// <summary>
        /// No question is asked about the stimulus. Unclassified stimuli are included in the experiment
        /// </summary>
        [Description("No question is asked about the stimulus. Unclassified stimuli are included in the experiment")]
        None,
    }

    /// <summary>
    /// General parameters for an experiment
    /// </summary>
    [Serializable]
    [Description("Experiment configuration", DisplayName = "Settings")]
    public class ExperimentSettings
    {
        /// <summary>
        /// The file extension used when serializing this class
        /// </summary>
        public const string EXTENSION = ".experiment";

        /// <summary>
        /// The name of the experiment
        /// </summary>
        [Parameter("The name of the experiment", DisplayName = "Experiment Name", DefaultValue = "")]
        public string ExperimentName { get; set; }

        /// <summary>
        /// The subject's name
        /// </summary>
        [Parameter("The subject's name", DisplayName = "Subject Name")]
        public string SubjectName { get; set; }

        /// <summary>
        /// The amount of time (in ms) for which each image is displayed
        /// </summary>
        [Parameter("The amount of time (in ms) for which each image is displayed", DisplayName = "Display Time", DefaultValue = 500, MinValue = 0)]
        public int DisplayTime { get; set; }

        /// <summary>
        /// The rest time (in ms) between the displaying of each image
        /// </summary>
        [Parameter("The rest time (in ms) between the displaying of each image", DisplayName = "Rest Time", DefaultValue = 500, MinValue = 0)]
        public int RestTime { get; set; }

        /// <summary>
        /// The amount of time (in ms) for which the fixation cross is displayed
        /// </summary>
        [Parameter("The amount of time (in ms) for which the fixation cross is displayed", DisplayName = "Fixation Time", DefaultValue = 500, MinValue = 0)]
        public int FixationTime { get; set; }

        /// <summary>
        /// The amount of time (in ms) for which the instructions are displayed
        /// </summary>
        [Parameter("The amount of time (in ms) for which the instructions are displayed", DisplayName = "Instruction Time", DefaultValue = 1500, MinValue = 0)]
        public int InstructionTime { get; set; }

        /// <summary>
        /// The number of images of each class dislplayed in training mode
        /// </summary>
        [Parameter("The number of images of each class dislplayed in training mode", DisplayName = "Training Images Per Class", DefaultValue = 1, MinValue = 1)]
        public int TrainingImagesPerClass { get; set; }

        /// <summary>
        /// The total number of images displayed in test mode
        /// </summary>
        [Parameter("The total number of images displayed in test mode", DisplayName = "Test Images", DefaultValue = 1, MinValue = 0)]
        public int TestImages { get; set; }

        /// <summary>
        /// The number of test stimuli displayed before the classifiers are re-trained
        /// </summary>
        [Parameter("The number of test stimuli displayed before the classifiers are re-trained", DisplayName = "Training Frequency", DefaultValue = 10, MinValue = 1)]
        public int TrainFrequency { get; set; }

        /// <summary>
        /// Determines whether a question is asked about each stimulus and how the answer to that question is interpreted
        /// </summary>
        [Parameter("Determines whether a question is asked about each stimulus and how the answer to that question is interpreted", DisplayName = "Question Mode", DefaultValue = QuestionMode.AskAndVerify)]
        public QuestionMode QuestionMode { get; set; }

        /// <summary>
        /// In side-by-side mode, should the user be told which class of image will appear on each side of the screen (so that he or she can anticipate viewing that class of image)?
        /// </summary>
        [Parameter("In side-by-side mode, should the user be told which class of image will appear on each side of the screen (so that he or she can anticipate viewing that class of image)?",
            DisplayName = "Allow Anticipation", DefaultValue = true)]
        public bool AllowAnticipation { get; set; }

        /// <summary>
        /// Should the application create a log of the experiment?
        /// </summary>
        [Parameter("Should " + GUIUtils.Strings.APP_NAME + " create a log of the experiment?", DisplayName = "Log Experiment", DefaultValue = false)]
        public bool LogExperiment { get; set; }

        /// <summary>
        /// Should the application save labeled trial EEG data collected during the experiment?
        /// </summary>
        [Parameter("Should " + GUIUtils.Strings.APP_NAME + " save labeled trial EEG data collected during the experiment?", DisplayName = "Save Trial Data", DefaultValue = false)]
        public bool SaveTrialData { get; set; }

        /// <summary>
        /// Should the application save the raw EEG data collected during the experiment?
        /// </summary>
        [Parameter("Should " + GUIUtils.Strings.APP_NAME + " save the raw EEG data collected during the experiment?", DisplayName = "Save Raw Data", DefaultValue = false)]
        public bool SaveRawData { get; set; }

        /// <summary>
        /// The folder where logs and data files are saved. Not a parameter
        /// </summary>
        public string OutputFolder { get; set; }

        /// <summary>
        /// The image display settings object for the experiment. Not a parameter
        /// </summary>
        public ImageDisplaySettings ImageDisplaySettings { get; set; }

        /// <summary>
        /// The artifact detection settings object for the experiment. Not a parameter
        /// </summary>
        public ArtifactDetectionSettings ArtifactDetectionSettings { get; set; }

        /// <summary>
        /// Construct a settings object with the default parameter values
        /// </summary>
        public ExperimentSettings() { this.SetParametersToDefaultValues(); }

        /// <summary>
        /// Returns a loggable string representation of the settings object
        /// </summary>
        public override string ToString()
        {
            return this.GetParameters()
                .Select(p => p.DisplayName + "=" + this.GetProperty(p.Property))
                .Then("Output Folder=" + this.OutputFolder)
                .Then("Image Display Settings {")
                .Concat(this.ImageDisplaySettings
                    .GetParameters()
                    .Select(ip => "\t" + ip.DisplayName + "=" + this.ImageDisplaySettings.GetProperty(ip.Property)))
                .Then("}")
                .ConcatToString(Environment.NewLine);
        }
    }
}
