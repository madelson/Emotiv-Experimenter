using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// Implements a view that finishes after a fixed time limit. A negative display time
    /// causes the view to display forever. Does not set a result.
    /// </summary>
    public abstract class AbstractTimedView : View
    {
        /// <summary>
        /// Constructs a timed view with the given display time. A negative display time
        /// value causes the view to display indefinitely.
        /// </summary>
        public AbstractTimedView(int displayTimeMillis)
            : base()
        {
            if (displayTimeMillis < 0)
                return;

            var timer = this.RegisterDisposable(new Timer() { Interval = displayTimeMillis, Enabled = false });
            timer.Tick += (sender, args) => { this.Finish(); };
            this.DoOnDeploy(c => timer.Enabled = true);
        }
    }
}
