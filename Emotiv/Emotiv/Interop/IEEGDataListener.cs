using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;

namespace MCAEmotiv.Interop
{
    /// <summary>
    /// An interface for objects that consume EEG data
    /// </summary>
    public interface IEEGDataListener : IDisposable
    {
        /// <summary>
        /// Called when new data arrives
        /// </summary>
        void Listen(IArrayView<EEGDataEntry> data);

        /// <summary>
        /// Called when the data source connects
        /// </summary>
        void SourceConnected(IEEGDataSource source);

        /// <summary>
        /// Called when the data source disconnects
        /// </summary>
        void SourceDisconnected(IEEGDataSource source);
    }

    /// <summary>
    /// Consumes EEG data using actions which are invoked using an ISynchronizeInvoke.
    /// Null actions are ignored
    /// </summary>
    public class EEGDataListener : SafeDisposable, IEEGDataListener
    {
        private readonly ISynchronizeInvoke invoker;
        private readonly Action<IArrayView<EEGDataEntry>> onListen;
        private readonly Action onDispose;
        private readonly Action<IEEGDataSource> onSourceConnected, onSourceDisconnected;
        private volatile IEEGDataSource source = null;

        /// <summary>
        /// Create a listener whose behavior is defined by the given actions. Any action may be null
        /// </summary>
        public EEGDataListener(ISynchronizeInvoke invoker,
            Action<IEEGDataSource> onSourceConnected,
            Action<IArrayView<EEGDataEntry>> onListen,
            Action<IEEGDataSource> onSourceDisconnected,
            Action onDispose = null)
        {
            this.invoker = invoker;
            this.onSourceConnected = onSourceConnected;
            this.onListen = onListen;
            this.onSourceDisconnected = onSourceDisconnected;
            this.onDispose = onDispose;
        }

        /// <summary>
        /// Removes the listener from its data source, and invokes the dispose action
        /// </summary>
        protected override void DisposeOfManagedResources()
        {
            if (this.source != null)
                this.source.RemoveListener(this);

            if (this.onDispose != null)
                this.invoker.BeginInvoke(this.onDispose);
        }

        /// <summary>
        /// Invokes the listen action
        /// </summary>
        public void Listen(IArrayView<EEGDataEntry> data)
        {
            if (this.onListen != null)
                this.invoker.BeginInvoke(this.onListen, new object[] { data });
        }

        /// <summary>
        /// Invokes the connected action
        /// </summary>
        public void SourceConnected(IEEGDataSource source)
        {
            if (this.source == null)
                this.source = source;
            else if (this.source != source)
                throw new Exception("Listener connected to multiple sources!");

            if (this.onSourceConnected != null)
                this.invoker.BeginInvoke(() => this.onSourceConnected(source));
        }

        /// <summary>
        /// Invokes the disconnected action
        /// </summary>
        public void SourceDisconnected(IEEGDataSource source)
        {
            if (this.source == null)
                this.source = source;
            else if (this.source != source)
                throw new Exception("Listener connected to multiple sources!");

            if (this.onSourceDisconnected != null)
                this.invoker.BeginInvoke(() => this.onSourceDisconnected(source));
        }
    }
}
