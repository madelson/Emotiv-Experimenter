using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCAEmotiv.GUI.Controls
{
    /// <summary>
    /// A custom tab which provides a tooltip and is closeable (provided it is used with a custom tab control)
    /// </summary>
    public class CustomTab : TabPage
    {
        /// <summary>
        /// Fires as the tab closes
        /// </summary>
        public event TabControlCancelEventHandler Closing;
        
        private ToolTip tooltip = null;
        /// <summary>
        /// Retrieves the tooltip associated with the tab
        /// </summary>
        public ToolTip ToolTip
        {
            get
            {
                if (this.tooltip == null)
                    this.tooltip = new ToolTip();
                return this.tooltip;
            }
        }

        /// <summary>
        /// Raises the tab's closing event
        /// </summary>
        public void RaiseClosingSafe(TabControlCancelEventArgs args)
        {
            if (this.Closing != null)
                this.Closing(this, args);
        }

        /// <summary>
        /// Closes the tab
        /// </summary>
        public void Close()
        {
            var parent = this.Parent as CustomTabControl;
            if (parent == null)
                return;

            int index;
            for (index = 0; index < parent.TabPages.Count; index++)
                if (parent.TabPages[index] == this)
                    break;

            var args = new TabControlCancelEventArgs(this, index, false, TabControlAction.Selected);
            this.Closing(parent, args);
            if (args.Cancel)
            {
                parent.TabPages.Remove(this);
                this.Dispose();
            }
        }

        /// <summary>
        /// Disposes the tab
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.DisposeOfManagedResources();
                if (this.tooltip != null)
                    this.tooltip.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Can be overriden by subclasses to add additional dispose logic
        /// </summary>
        protected virtual void DisposeOfManagedResources()
        {
        }
    }
}
