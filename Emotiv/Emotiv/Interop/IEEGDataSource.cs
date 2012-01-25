using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MCAEmotiv.Interop
{
    /// <summary>
    /// An interface for a source of EEG data
    /// </summary>
    public interface IEEGDataSource : IDisposable
    {
        /// <summary>
        /// Gets or sets the marker being used to annotate the incoming data.
        /// </summary>
        int Marker { get; set; }

        /// <summary>
        /// Add a listener to this data source, unless it is already listening to this source.
        /// New listeners are always sent a disconnect or connect message upon being added, and are
        /// always set a connect message before any data is delivered to them.
        /// </summary>
        void AddListener(IEEGDataListener listener);

        /// <summary>
        /// Adds each listener. All listeners receive the same messages (i. e. they are added simultaneously).
        /// </summary>
        void AddListeners(IEnumerable<IEEGDataListener> listeners);

        /// <summary>
        /// Removes a listener from this data source
        /// </summary>
        void RemoveListener(IEEGDataListener listener);

        /// <summary>
        /// Removes each listener. All listeners receive the same messages.
        /// </summary>
        void RemoveListeners(IEnumerable<IEEGDataListener> listeners);
    }

    /// <summary>
    /// An abstract base class for data sources
    /// </summary>
    public abstract class AbstractEEGDataSource : SafeDisposable, IEEGDataSource
    {
        private const int READ_INTERVAL_MILLIS = 100;

        /// <summary>
        /// The synchronization root for this object
        /// </summary>
        protected abstract object Lock { get; }
        private readonly List<IEEGDataListener> listeners = new List<IEEGDataListener>();
        private readonly Thread readerThread;
        private bool hasStarted = false, isOnline = false;
        private volatile bool shouldStop = false;

        /// <summary>
        /// Construct a data source with its own reader thread
        /// </summary>
        public AbstractEEGDataSource()
        {
            this.readerThread = new Thread(this.ReadLoop) { IsBackground = true, Name = "EEG Polling Thread" };
        }

        private void ReadLoop()
        {
            //this.hasStarted = true;

            IArrayView<EEGDataEntry> data;
            for (bool firstTime = true; !this.shouldStop; firstTime = false)
            {
                lock (this.Lock)
                {
                    if (this.isOnline)
                    {
                        if (this.isOnline = this.TryGetData(out data))
                            foreach (var listener in this.listeners)
                                listener.Listen(data);
                        else
                            foreach (var listener in this.listeners)
                                listener.SourceDisconnected(this);
                    }
                    else if (this.isOnline = this.TryGetData(out data))
                        foreach (var listener in this.listeners)
                        {
                            listener.SourceConnected(this);
                            listener.Listen(data);
                        }
                    else if (firstTime)
                        foreach (var listener in this.listeners)
                            listener.SourceDisconnected(this);
                }

                Thread.Sleep(READ_INTERVAL_MILLIS);
            }

            lock (this.Lock)
                this.DisposeHelper();
        }

        /// <summary>
        /// Sends a message to the internal reader thread to halt its execution
        /// </summary>
        protected override void DisposeOfManagedResources()
        {
            this.shouldStop = true;

            // start the reader thread if it hasn't started so that
            // everything disposes properly
            lock (this.Lock)
                if (!this.hasStarted)
                    this.readerThread.Start();
        }

        /// <summary>
        /// Gets or sets the marker field of incoming entries
        /// </summary>
        public abstract int Marker { get; set; }

        /// <summary>
        /// Add a listener to the data source
        /// </summary>
        public void AddListener(IEEGDataListener listener)
        {
            this.AddListeners(listener.Enumerate());
        }

        /// <summary>
        /// Adds each of the listeners to the data source simultaneously
        /// </summary>
        public void AddListeners(IEnumerable<IEEGDataListener> listeners)
        {
            lock (this.Lock)
            {
                this.listeners.AddRange(listeners);
                if (this.hasStarted)
                    foreach (var listener in listeners)
                        if (this.isOnline)
                            listener.SourceConnected(this);
                        else
                            listener.SourceDisconnected(this);
                else
                {
                    this.hasStarted = true;
                    this.readerThread.Start();
                }
            }
        }

        /// <summary>
        /// Removes a listener from the data source
        /// </summary>
        public void RemoveListener(IEEGDataListener listener)
        {
            this.RemoveListeners(listener.Enumerate());
        }

        /// <summary>
        /// Removes each of the listeners from the data source simultaneously.
        /// </summary>
        public void RemoveListeners(IEnumerable<IEEGDataListener> listeners)
        {
            lock (this.Lock)
                this.listeners.RemoveAll(listeners.Contains);
        }

        /// <summary>
        /// Called from the reader thread in a locked context.
        /// </summary>
        protected abstract bool TryGetData(out IArrayView<EEGDataEntry> data);

        /// <summary>
        /// Called from the reader thread in a locked context.
        /// </summary>
        protected abstract void DisposeHelper();
    }

    /// <summary>
    /// Generates random data for use in testing the application when the headset is not connected
    /// </summary>
    public class MockEEGDataSource : AbstractEEGDataSource
    {
        private DateTime baseTime = DateTime.Now, markerBaseTime = DateTime.Now;

        /// <summary>
        /// A synchronization root for this class
        /// </summary>
        protected override object Lock
        {
            get { return this; }
        }

        private int marker = EEGDataEntry.MARKER_DEFAULT;
        /// <summary>
        /// Gets or sets the data source's marker
        /// </summary>
        public override int Marker
        {
            get
            {
                lock (this.Lock)
                    return this.marker;
            }
            set
            {
                lock (this.Lock)
                {
                    this.marker = value;
                    this.markerBaseTime = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Attempts to get data from the data source
        /// </summary>
        protected override bool TryGetData(out IArrayView<EEGDataEntry> data)
        {
            var rand = new Random();

            data = (10)
                .CountTo()
                .Select(i => new EEGDataEntry(this.Marker,
                        (DateTime.Now - baseTime).TotalMilliseconds.Rounded(),
                        (DateTime.Now - this.markerBaseTime).TotalMilliseconds.Rounded(),
                        Channels.Values.Count
                            .CountTo()
                            .Select(j => 3000 + 50 * rand.NextDouble()))).ToIArray();
            return true;
        }

        /// <summary>
        /// No-op
        /// </summary>
        protected override void DisposeHelper()
        {
        }
    }
}
