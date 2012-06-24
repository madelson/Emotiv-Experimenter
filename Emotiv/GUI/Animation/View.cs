using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// Represents an asynchronous result value for a view.
    /// </summary>
    public interface IViewResult
    {
        /// <summary>
        /// Retrieves the result's value
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Does the result have a value?
        /// </summary>
        bool HasValue { get; }
    }

    /// <summary>
    /// Represents a view to be deployed to a Control.
    /// </summary>
    public abstract class View : SafeDisposable
    {
        #region ---- IViewResult Implementations ----
        private class ViewResult : IViewResult
        {
            private bool hasValue = false;
            private object value;

            public object Value
            {
                get
                {
                    lock (this)
                    {
                        if (!this.hasValue)
                            throw new Exception("Result not available!");

                        return this.value;
                    }
                }
                set
                {
                    lock (this)
                    {
                        if (this.hasValue)
                           throw new Exception("Result already set!");

                        this.value = value;
                        this.hasValue = true;
                    }
                }
            }

            public bool HasValue
            {
                get { lock (this) { return this.hasValue; } }
            }
        }
        #endregion

        private readonly ViewResult result = new ViewResult();
        /// <summary>
        /// Retrieves the result for this view
        /// </summary>
        public IViewResult Result { get { return this.result; } }

        private Control container = null;
        private ManualResetEvent onStop = null;
        private bool isDeployed = false;
        private readonly List<View> subViews = new List<View>();
        private readonly Stack<IDisposable> disposables = new Stack<IDisposable>();
        private readonly Stack<Action<Control>> deployActions = new Stack<Action<Control>>();
        private readonly Stack<Action> finishActions = new Stack<Action>();
        private View owner = null;

        /// <summary>
        /// Deploys the view. Must be called from the GUI thread.
        /// </summary>
        public void DeployTo(Control container, ManualResetEvent onStop)
        {
            this.CheckIfDisposed();
            if (this.container != null || this.onStop != null)
                throw new Exception("A view cannot be deployed twice!");

            this.container = container;
            this.onStop = onStop;
            this.isDeployed = true;
            while (this.deployActions.Count > 0)
                this.deployActions.Pop()(this.container);
        }

        /// <summary>
        /// This method must be called from the GUI thread.
        /// </summary>
        protected void Finish()
        {
            if (!this.isDeployed)
                return;

            // finish any subviews
            foreach (var subView in this.subViews)
                subView.Finish();

            // finish this
            while (this.finishActions.Count > 0)
                this.finishActions.Pop()();
            // guarantee that the result has a value!
            if (!this.result.HasValue)
                this.SetResult(null);
            this.isDeployed = false;

            // have the owner finish, or else just stop
            if (this.owner != null)
                this.owner.Finish();
            else if (this.onStop != null)
                this.onStop.Set();
        }

        /// <summary>
        /// This method (and thus Dispose) can be called from any thread.
        /// </summary>
        protected override void DisposeOfManagedResources()
        {
            new Action(() =>
            {
                // perform all finishes
                this.Finish();

                // dispose of all subviews
                foreach (var subView in this.subViews)
                    subView.Dispose();

                // dispose of this
                if (this.container != null)
                    this.container.Controls.Clear();
                while (this.disposables.Count > 0)
                    this.disposables.Pop().Dispose();
            }).GUIInvoke();
        }

        /// <summary>
        /// Implementations may call this method before calling Finish() in order to set
        /// the result value for the view.
        /// </summary>
        protected void SetResult(object value)
        {
            this.result.Value = value;
        }

        /// <summary>
        /// Deploys subView to container, with subView using the same onStop event
        /// as this view. The subView will automatically be disposed of when this view
        /// is disposed of. This should be called during deployment.
        /// </summary>
        protected void DeploySubView(View subView, Control container, bool causesOwnerToFinish = true)
        {
            if (!this.isDeployed)
                throw new Exception("View not deployed!");

            subView.owner = causesOwnerToFinish ? this : null;
            this.subViews.Add(subView);
            subView.DeployTo(container, causesOwnerToFinish ? this.onStop : null);
        }

        /// <summary>
        /// Pushes the object onto a stack of disposable objects which will be disposed
        /// in LIFO order when the view is disposed. Returns the object for convenience.
        /// </summary>
        protected T RegisterDisposable<T>(T disposable)
            where T : IDisposable
        {
            this.disposables.Push(disposable);
            return disposable;
        }

        /// <summary>
        /// Pushes the action onto a stack which will be executed in LIFO order immediately before
        /// the view finishes.
        /// </summary>
        public void DoOnFinishing(Action action)
        {
            this.finishActions.Push(action);
        }

        /// <summary>
        /// Pushes the action onto a stack which will be executed in LIFO order upon deploying this
        /// view.
        /// </summary>
        public void DoOnDeploy(Action<Control> action)
        {
            this.deployActions.Push(action);
        }

        /// <summary>
        /// Invokes the action provided that the view is deployed. Returns true if the action
        /// was invoked.
        /// </summary>
        protected bool Invoke(Action action)
        {
            bool invoked = false;
            new Action(() =>
            {
                if (this.isDeployed)
                {
                    invoked = true;
                    action();
                }
            }).GUIInvoke();

            return invoked;
        }
    }
}
