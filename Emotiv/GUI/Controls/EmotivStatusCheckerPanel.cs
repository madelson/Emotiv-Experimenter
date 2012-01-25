using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MCAEmotiv.Interop;
using System.Drawing;

namespace MCAEmotiv.GUI.Controls
{
    /// <summary>
    /// A panel which displays the connection status of the Emotiv headset
    /// </summary>
    public class EmotivStatusCheckerPanel : Panel
    {
        /// <summary>
        /// Is the headset connected?
        /// </summary>
        public bool HeadsetConnected { get; private set; }
        private IEEGDataListener listener;

        /// <summary>
        /// Constructs a panel
        /// </summary>
        public EmotivStatusCheckerPanel()
            : base()
        {
            this.HeadsetConnected = false;

            var label = new Label() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            this.Controls.Add(label);
            
            this.listener = new EEGDataListener(GUIUtils.GUIInvoker,
                (ignored) => 
                { 
                    this.HeadsetConnected = true;
                    label.Text = "Headset connected!";
                    label.BackColor = Color.Green;
                    label.ForeColor = Color.Black;
                },
                null,
                (ignored) => 
                {
                    this.HeadsetConnected = false;
                    label.Text = "Could not connect to headset"; 
                    label.BackColor = Color.Red; 
                    label.ForeColor = Color.White; 
                });
            EmotivDataSource.Instance.AddListener(this.listener);
        }

        /// <summary>
        /// Disposes the control
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            EmotivDataSource.Instance.RemoveListener(this.listener);
            this.listener.Dispose();
            base.Dispose(disposing);
        }
    }
}
