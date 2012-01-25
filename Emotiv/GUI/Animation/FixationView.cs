using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using MCAEmotiv.GUI.Configurations;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// This view draws a fixation cross and waits for a fixed time period
    /// </summary>
    public class FixationView : AbstractTimedView
    {
        /// <summary>
        /// Should two fixation crosses be displayed?
        /// </summary>
        public bool SplitView { get; set; }

        /// <summary>
        /// The maximum image size (used to calculate the split view locations)
        /// </summary>
        public Size MaxImageSize { get; set; }

        /// <summary>
        /// Constructs a fixation view that displays for the specified time period
        /// </summary>
        public FixationView(int displayTimeMillis)
            : base(displayTimeMillis)
        {
            this.MaxImageSize = new ImageDisplaySettings().ImageSize;
            var panel = this.RegisterDisposable(new Panel() { Dock = DockStyle.Fill });
            panel.Paint += (sender, args) =>
            {
                if (this.SplitView)
                {
                    Rectangle rectangle1, rectangle2;
                    GUIUtils.GetSplitModeImageRectangles(panel.ClientRectangle, this.MaxImageSize, out rectangle1, out rectangle2);
                    args.Graphics.DrawFixationCross(rectangle1.Center());
                    args.Graphics.DrawFixationCross(rectangle2.Center());
                }
                else
                    args.Graphics.DrawFixationCross(panel.ClientRectangle.Center());
            };
            this.DoOnDeploy(c => c.Controls.Add(panel));
        }

        /// <summary>
        /// Constructs a fixation view that displays for the specified time period and 
        /// returns the result as an out parameter
        /// </summary>
        public FixationView(int displayTimeMillis, out IViewResult result)
            : this(displayTimeMillis)
        {
            result = this.Result;
        }
    }
}
