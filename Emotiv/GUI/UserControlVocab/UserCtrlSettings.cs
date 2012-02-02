using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.GUI.Configurations;

namespace MCAEmotiv.GUI.UserControlVocab
{
    /// <summary>
    /// General parameters for an experiment
    /// </summary>
    [Serializable]
    [Description("Experiment configuration", DisplayName = "Settings")]
    public class UserCtrlSettings
    {
        /// <summary>
        /// The file extension used when serializing this class
        /// </summary>
        public const string EXTENSION = ".user";

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
        /// The amount of time (in ms) for which each image is displayed in study phase
        /// </summary>
        [Parameter("The amount of time (in ms) for which each image is displayed during the study phase", DisplayName = "Presentation Time", DefaultValue = 3000, MinValue = 0)]
        public int PresentationTime { get; set; }

        /// <summary>
        /// The amount of time (in ms) for which each image is displayed in practice phase
        /// </summary>
        [Parameter("The amount of time (in ms) for which each image is displayed during the practice phase", DisplayName = "Display Time", DefaultValue = 3000, MinValue = 0)]
        public int DisplayTime { get; set; }

        /// <summary>
        /// The amount of time (in ms) before the subject can answer in practice phase
        /// </summary>
        [Parameter("The amount of time (in ms) before the subject can answer during the practice phase", DisplayName = "Delay Time", DefaultValue = 1500, MinValue = 0)]
        public int DelayTime { get; set; }

        /// <summary>
        /// The amount of time (in ms) for which feedback about the answer is displayed in practice phase
        /// </summary>
        [Parameter("The amount of time (in ms) for which feedback about the answer is displayed during the practice phase", DisplayName = "Feedback Time", DefaultValue = 500, MinValue = 0)]
        public int FeedbackTime { get; set; }

        /// <summary>
        /// The rest time (in ms) between the displaying of each image in the study phase
        /// </summary>
        [Parameter("The rest time (in ms) between the displaying of each image in the study phase", DisplayName = "Rest Time", DefaultValue = 1500, MinValue = 0)]
        public int RestTime { get; set; }

        /// <summary>
        /// The time (in ms) when it is safe to blink during EEG recording
        /// </summary>
        [Parameter("The time (in ms) when it is safe to blink during EEG recording", DisplayName = "Blink Time", DefaultValue = 1500, MinValue = 0)]
        public int BlinkTime { get; set; }

        /// <summary>
        /// The amount of time (in ms) for which the fixation cross is displayed
        /// </summary>
        [Parameter("The amount of time (in ms) for which the fixation cross is displayed", DisplayName = "Fixation Time", DefaultValue = 1500, MinValue = 0)]
        public int FixationTime { get; set; }

        /// <summary>
        /// The amount of time (in ms) for which the instructions are displayed
        /// </summary>
        [Parameter("The amount of time (in ms) for which the instructions are displayed", DisplayName = "Instruction Time", DefaultValue = 1500, MinValue = 0)]
        public int InstructionTime { get; set; }

        /// <summary>
        /// The number of Study-Practice Trials
        /// </summary>
        [Parameter("The number of Trials", DisplayName = "Number Trials", DefaultValue= 40, MinValue = 1)]
        public int NumTrials {get; set; }

        /// <summary>
        /// Should the application create a log of the experiment?
        /// </summary>
        [Parameter("Should " + GUIUtils.Strings.APP_NAME + " create a log of the experiment?", DisplayName = "Log Experiment", DefaultValue = false)]
        public bool LogExperiment { get; set; }

        ///// <summary>
        ///// Should the application save labeled trial EEG data collected during the experiment?
        ///// </summary>
        //[Parameter("Should " + GUIUtils.Strings.APP_NAME + " save labeled trial EEG data collected during the experiment?", DisplayName = "Save Trial Data", DefaultValue = false)]
        //public bool SaveTrialData { get; set; }

        ///// <summary>
        ///// Should the application save the raw EEG data collected during the experiment?
        ///// </summary>
        //[Parameter("Should " + GUIUtils.Strings.APP_NAME + " save the raw EEG data collected during the experiment?", DisplayName = "Save Raw Data", DefaultValue = false)]
        //public bool SaveRawData { get; set; }

        /// <summary>
        /// The folder where logs and data files are saved. Not a parameter
        /// </summary>
        public string OutputFolder { get; set; }

        /// <summary>
        /// The file from which Study stimuli are read. Not a parameter
        /// </summary>
        public string PresentationFile { get; set; }

        /// <summary>
        /// The file from which Test stimuli are read. Not a parameter
        /// </summary>
        public string TestFile { get; set; }

        /// <summary>
        /// The file from which the answers to the Test stimuli are read. Not a parameter
        /// </summary>
        public string AnsFile { get; set; }

        ///// <summary>
        ///// The artifact detection settings object for the experiment. Not a parameter
        ///// </summary>
        //public ArtifactDetectionSettings ArtifactDetectionSettings { get; set; }

        /// <summary>
        /// Construct a settings object with the default parameter values
        /// </summary>
        public UserCtrlSettings() { this.SetParametersToDefaultValues(); }


        /// <summary>
        /// Returns a loggable string representation of the settings object
        /// </summary>
        public override string ToString()
        {
            return this.GetParameters()
                .Select(p => p.DisplayName + "=" + this.GetProperty(p.Property))
                .Then("Output Folder=" + this.OutputFolder)
                .Then("Image Display Settings {")
                .Then("}")
                .ConcatToString(Environment.NewLine);
        }
    }
}
