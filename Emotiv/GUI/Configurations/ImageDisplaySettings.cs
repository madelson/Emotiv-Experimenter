using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace MCAEmotiv.GUI.Configurations
{
    /// <summary>
    /// Image display configuration
    /// </summary>
    [Serializable]
    [Description("Image display configuration", DisplayName = "Settings")]
    public class ImageDisplaySettings
    {
        /// <summary>
        /// The maximum width (in pixels) of the displayed image during an experiment
        /// </summary>
        [Parameter("The maximum width (in pixels) of the displayed image during an experiment", DisplayName = "Width", DefaultValue = 400, MinValue = 1)]
        public int ImageWidth { get; set; }

        /// <summary>
        /// The maximum height (in pixels) of the displayed image during an experiment
        /// </summary>
        [Parameter("The maximum height (in pixels) of the displayed image during an experiment", DisplayName = "Height", DefaultValue = 400, MinValue = 1)]
        public int ImageHeight { get; set; }

        /// <summary>
        /// The transparency value [0 - 255] of the overlaid image
        /// </summary>
        [Parameter("The transparency value [0 - 255] of the overlaid image", DefaultValue = 127, MinValue = 0, MaxValue = 255)]
        public int Alpha { get; set; }

        /// <summary>
        /// Should the images be superimposed or displayed side by side?
        /// </summary>
        [Parameter("Should the images be superimposed or displayed side by side?", DisplayName = "Superimpose", DefaultValue = false)]
        public bool SuperimposeImages { get; set; }

        /// <summary>
        /// Should the images be displayed in black and white?
        /// </summary>
        [Parameter("Should the images be displayed in black and white?", DisplayName = "Grayscale", DefaultValue = true)]
        public bool UseGrayscale { get; set; }

        /// <summary>
        /// Should a tone be played at the end of each trial?
        /// </summary>
        [Parameter("Should a tone be played at the end of each trial?", DisplayName = "Beep After Display", DefaultValue = false)]
        public bool Beep { get; set; }

        /// <summary>
        /// Cycle randomly through pairs of images in the selected classes (this affects the main display, not the experiment)
        /// </summary>
        [Parameter("Cycle randomly through pairs of images in the selected classes (this affects the main display, not the experiment)", DisplayName = "Cycle Through Images", DefaultValue = true)]
        public bool CycleThroughImages { get; set; }

        /// <summary>
        /// Retrieves the size of the image based on the height and width parameters. Not a parameter
        /// </summary>
        public Size ImageSize { get { return new Size(this.ImageWidth, this.ImageHeight); } }

        /// <summary>
        /// Construct a settings object with the default parameter values
        /// </summary>
        public ImageDisplaySettings() { this.SetParametersToDefaultValues(); }
    }
}
