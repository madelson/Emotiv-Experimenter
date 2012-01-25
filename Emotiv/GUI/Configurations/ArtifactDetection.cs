using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.Interop;
using MCAEmotiv.Analysis;

namespace MCAEmotiv.GUI.Configurations
{
    /// <summary>
    /// Contains configuration information for the artifact detection algorithm
    /// </summary>
    [Serializable]
    [Description("Configuration for the movement-artifact detection algorithm" + DESCRIPTION_SUPPLEMENT, DisplayName = "Artifact Detection Settings")]
    public class ArtifactDetectionSettings
    {
        private const string DESCRIPTION_SUPPLEMENT = "\r\n\r\n" + GUIUtils.Strings.APP_NAME +
@" uses a two-pass algorithm to detect movement artifacts.
The algorithm detects a movement artifact when the difference
between the moving average of the voltages and the average voltage
on the selected channel passes some threshold value. The moving 
average has one parameter, ALPHA, which is the weight of the 
current voltage value in determining the average. The first pass 
uses Alpha 1 and Threshold 1 to detect artifacts. The resultant 
moving average is then subtracted from the time series and a 
second pass is run using Alpha 2 and Threshold 2";

        /// <summary>
        /// Should the experiment use artifact detection?
        /// </summary>
        [Parameter("Should artifact detection be used to actively reject trials?" + DESCRIPTION_SUPPLEMENT, DisplayName = "Use Artifact Detection", DefaultValue = true)]
        public bool UseArtifactDetection { get; set; }

        /// <summary>
        /// The moving average parameter on the first pass
        /// </summary>
        [Parameter("The moving average parameter on the first pass" + DESCRIPTION_SUPPLEMENT, DisplayName = "Alpha 1", DefaultValue = 0.025, MinValue = 0.0, MaxValue = 1.0)]
        public double Alpha1 { get; set; }

        /// <summary>
        /// The threshold parameter on the first pass (uV)
        /// </summary>
        [Parameter("The threshold parameter on the first pass (uV)" + DESCRIPTION_SUPPLEMENT, DisplayName = "Threshold 1", DefaultValue = 40.0, MinValue = 0.0)]
        public double Threshold1 { get; set; }

        /// <summary>
        /// The moving average parameter on the second pass
        /// </summary>
        [Parameter("The moving average parameter on the second pass" + DESCRIPTION_SUPPLEMENT, DisplayName = "Alpha 2", DefaultValue = 0.5, MinValue = 0.0, MaxValue = 1.0)]
        public double Alpha2 { get; set; }

        /// <summary>
        /// The threshold parameter on the second pass (uV)
        /// </summary>
        [Parameter("The threshold parameter on the second pass (uV)" + DESCRIPTION_SUPPLEMENT, DisplayName = "Threshold 2", DefaultValue = 40.0, MinValue = 0.0)]
        public double Threshold2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Parameter("The channel used to perform artifact detection" + DESCRIPTION_SUPPLEMENT, DisplayName = "Channel", DefaultValue = Channel.AF3)]
        public Channel Channel { get; set; }

        /// <summary>
        /// Should the algorithm also test the selected channel's "mirror" channel on the other side of the head?
        /// </summary>
        [Parameter("Should the algorithm also test the selected channel's \"mirror\" channel on the other side of the head?" + DESCRIPTION_SUPPLEMENT,
            DisplayName = "Use Mirror", DefaultValue = false)]
        public bool UseMirror { get; set; }

        /// <summary>
        /// Play a tone upon detecting an artifact (this affects the main display, not the experiment)
        /// </summary>
        [Parameter("Play a tone upon detecting an artifact (this affects the main display, not the experiment)", DisplayName = "Beep On Detection", DefaultValue = false)]
        public bool Beep { get; set; }

        /// <summary>
        /// Construct a settings object with the default parameter values
        /// </summary>
        public ArtifactDetectionSettings() { this.SetParametersToDefaultValues(); }

        /// <summary>
        /// Checks the trial for artifacts based on the current settings
        /// </summary>
        public bool HasMotionArtifact(IEnumerable<EEGDataEntry> trial)
        {
            return this.UseArtifactDetection && trial.HasMotionArtifact(this.Alpha1,
                this.Threshold1,
                this.Alpha2,
                this.Threshold2,
                this.Channel,
                this.UseMirror);
        }
    }
}
