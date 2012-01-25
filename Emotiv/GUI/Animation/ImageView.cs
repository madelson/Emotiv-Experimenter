using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using MCAEmotiv.GUI.Controls;
using MCAEmotiv.GUI.Configurations;
using MCAEmotiv.Interop;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// Displays one or more images using the specified settings. Optionally tags the display using
    /// an EEG data source, and resets the marker on finishing. The result is a boolean indicating 
    /// whether the images successfully loaded.
    /// </summary>
    public class ImageView : AbstractTimedView
    {
        /// <summary>
        /// The settings used to configure this view
        /// </summary>
        public ImageDisplaySettings ImageDisplaySettings { get; set; }

        /// <summary>
        /// The primary image or text to be displayed
        /// </summary>
        public string ImagePath { get; set; }

        /// <summary>
        /// The secondary image or text to be displayed
        /// </summary>
        public string SecondaryImagePath { get; set; }

        /// <summary>
        /// The data source to be marked
        /// </summary>
        public IEEGDataSource DataSource { get; set; }

        /// <summary>
        /// The marker with which to mark the data source while the image displays
        /// </summary>
        public int Marker { get; set; }

        /// <summary>
        /// Construct an image view that displays for the specified time
        /// </summary>
        public ImageView(int displayTimeMillis)
            : base(displayTimeMillis)
        {
            this.Marker = EEGDataEntry.MARKER_DEFAULT;
            var panel = this.RegisterDisposable(new ImagePanel() { Dock = DockStyle.Fill });
            this.DoOnDeploy(c =>  
            {
                panel.ImagePath = this.ImagePath;
                panel.SecondaryImagePath = this.SecondaryImagePath;
                this.SetResult(panel.ImagesLoaded);
                if (this.ImageDisplaySettings != null)
                    panel.Configure(this.ImageDisplaySettings);
                c.Controls.Add(panel);
                if (this.DataSource != null)
                    this.DataSource.Marker = this.Marker;
            });
            this.DoOnFinishing(() =>
            {
                // reset the marker on finishing
                if (this.DataSource != null)
                    this.DataSource.Marker = EEGDataEntry.MARKER_DEFAULT;
            });
        }

        /// <summary>
        /// Construct an image view that displays for the specified time and returns the result
        /// as an out parameter
        /// </summary>
        public ImageView(int displayTimeMillis, out IViewResult result)
            : this(displayTimeMillis)
        {
            result = this.Result;
        }
    }
}
