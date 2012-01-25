using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Media;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// Plays a sound when deployed. Displays nothing for 1ms
    /// </summary>
    public class SoundView : AbstractTimedView
    {
        /// <summary>
        /// Construct a sound view that plays the specified system sound
        /// </summary>
        public SoundView(SystemSound sound)
            : base(1)
        {
            this.DoOnDeploy(c => { sound.Play(); });
        }
    }
}
