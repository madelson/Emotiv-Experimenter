using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// Provides views to be shown by a visualizer
    /// </summary>
    public interface IViewProvider : IEnumerable<View>
    {
        /// <summary>
        /// The title of the animation
        /// </summary>
        string Title { get; }
    }
}
