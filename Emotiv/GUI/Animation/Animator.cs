using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// Animates a view provider in a seperate form
    /// </summary>
    public class Animator : SafeDisposable
    {
        private volatile bool shouldStop;
        private readonly Form form = new Form() { AutoSize = true, Size = new Size(750, 750), WindowState = FormWindowState.Maximized };
        private readonly ManualResetEvent onViewFinished = new ManualResetEvent(false);

        /// <summary>
        /// The size of the animation form
        /// </summary>
        public Size FormSize
        {
            get { return this.form.Size; }
            set { this.form.Size = value; }
        }

        /// <summary>
        /// Construct an animator
        /// </summary>
        public Animator()
        {
            this.form.FormClosing += (sender, args) => args.Cancel = true; // never really close
        }

        /// <summary>
        /// Start an animation with the specified provider. The on stop action is
        /// executed when the animation completes or when the user closes the animation form
        /// </summary>
        public void Start(IViewProvider provider, Action onStop)
        {
            this.CheckIfDisposed();
            if (this.form.Visible)
                throw new Exception("Animator is already running!");

            this.form.Text = provider.Title;
            var animationThread = new Thread(this.AnimationLoop) { Name = "Animation Loop Thread" };
            FormClosingEventHandler onClose = null;
            onClose = (sender, args) =>
            {              
                // close normally if the animation has finished
                if (!animationThread.IsAlive)
                {
                    form.Hide();
                    onStop(); // call onStop
                    form.FormClosing -= onClose; // remove the handler
                }
                // release the animation thread
                else if (!this.shouldStop)
                {
                    this.shouldStop = true;
                    this.onViewFinished.Set();
                    new Func<bool>(() => !animationThread.IsAlive).AsyncWaitOnGUIThread(() => this.form.Close());
                }
            };
            form.FormClosing += onClose;

            form.Controls.Clear();
            this.shouldStop = false;
            animationThread.Start(provider);
            this.form.Show();
        }

        /// <summary>
        /// Stops the animation
        /// </summary>
        public void Stop()
        {
            // note that this won't really close the form, but it will perform
            // the same behavior as when the user clicks close.
            this.form.Close();
        }

        private void AnimationLoop(object viewProvider)
        {
            IViewProvider provider = (IViewProvider)viewProvider;

            foreach (var view in provider)
            {
                // be sure to dispose of view
                using (view)
                {
                    new Action(() =>
                    {
                        if (this.shouldStop)
                            return;

                        this.onViewFinished.Reset();
                        view.DeployTo(this.form, this.onViewFinished);
                    }).GUIInvoke();

                    this.onViewFinished.WaitOne();
                }

                if (this.shouldStop)
                    break;
            }

            // close the form
            this.form.BeginInvoke(new Action(this.form.Close));
        }

        /// <summary>
        /// Disposes of all resources associated with the animator
        /// </summary>
        protected override void DisposeOfManagedResources()
        {
            new Action(this.Stop).GUIInvoke();
            this.form.Dispose();
            this.onViewFinished.Dispose();
        }
    }
}
