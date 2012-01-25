using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// A simple view that displays nothing for the specified time (or indefinitely, if
    /// time is negative).
    /// </summary>
    public class RestView : AbstractTimedView
    {
        /// <summary>
        /// Construct a rest view with the speficied display time
        /// </summary>
        public RestView(int restTimeMillis) : base(restTimeMillis) { }
    }
}
