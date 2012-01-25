using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    /// <summary>
    /// Implements the recommended disposable pattern with additional logic for
    /// preventing the object for being disposed twice.
    /// </summary>
    public abstract class SafeDisposable : IDisposable
    {
        private bool isDisposed = false;
        private object myLock = new object();

        /// <summary>
        /// Checks whether the object has been disposed. Note that this does not need to be
        /// checked before calling Dispose().
        /// </summary>
        public bool IsDisposed { get { lock (this.myLock) { return this.isDisposed; } } }

        /// <summary>
        /// Throws an exception if the object has been disposed.
        /// </summary>
        public void CheckIfDisposed()
        {
            if (this.IsDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
        }

        /// <summary>
        /// Calls Dispose(true)
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool disposeOfManagedResources)
        {
            lock (this)
            {
                if (this.isDisposed)
                    return;

                if (disposeOfManagedResources)
                    this.DisposeOfManagedResources();
                this.DisposeOfUnmanagedResources();
                this.isDisposed = true;
            }
        }

        /// <summary>
        /// Called before unmanaged resources are disposed.
        /// </summary>
        protected abstract void DisposeOfManagedResources();
        
        /// <summary>
        /// Called after managed resources are disposed.
        /// </summary>
        protected virtual void DisposeOfUnmanagedResources()
        {
        }

        /// <summary>
        /// Calls Dispose(false)
        /// </summary>
        ~SafeDisposable()
        {
            this.Dispose(false);
        }
    }
}
