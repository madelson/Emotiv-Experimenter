using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// Displays the given text for a fixed period of time
    /// </summary>
    public class TextView : AbstractTimedView
    {
        
        /// <summary>
        /// Construct a text view with the specified text and display time
        /// </summary>
        public TextView(string text, int displayTimeMillis, Font font = null)
            : base(displayTimeMillis)
        {
            var label = this.RegisterDisposable(new Label() 
            { 
                Dock = DockStyle.Fill, 
                Text = text, 
                TextAlign = ContentAlignment.MiddleCenter, 
                Margin = new Padding(10), 
                Font = font ?? GUIUtils.Constants.DISPLAY_FONT
            });
            this.DoOnDeploy(c => c.Controls.Add(label));
        }
    }
}
