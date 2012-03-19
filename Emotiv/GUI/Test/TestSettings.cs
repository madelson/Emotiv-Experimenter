using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.GUI.Configurations;

namespace MCAEmotiv.GUI.Test
{
    /// <summary>
    /// General parameters for an experiment
    /// </summary>
    [Serializable]
    [Description("Experiment configuration", DisplayName = "Settings")]
    public class TestSettings
    {
        /// <summary>
        /// The file extension used when serializing this class
        /// </summary>
        public const string EXTENSION = ".test";

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
        /// The amount of time (in ms) before the subject can answer
        /// </summary>
        [Parameter("The amount of time (in ms) before the subject can answer during the practice phase", DisplayName = "Delay Time", DefaultValue = 1500, MinValue = 0)]
        public int DelayTime { get; set; }

        /// <summary>
        /// The amount of time (in ms) the subject has to answer
        /// </summary>
        [Parameter("The amount of time (in ms) the subject has to answer", DisplayName = "Display Time", DefaultValue = 1500, MinValue = 0)]
        public int DisplayTime { get; set; }

                /// <summary>
        /// Should the application create a log of the experiment?
        /// </summary>
        [Parameter("Should " + GUIUtils.Strings.APP_NAME + " create a log of the experiment?", DisplayName = "Log Experiment", DefaultValue = false)]
        public bool LogExperiment { get; set; }

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

        /// <summary>
        /// Construct a settings object with the default parameter values
        /// </summary>
        public TestSettings() { this.SetParametersToDefaultValues(); }


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
