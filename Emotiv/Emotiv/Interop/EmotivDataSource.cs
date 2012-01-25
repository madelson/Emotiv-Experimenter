using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Emotiv;

namespace MCAEmotiv.Interop
{
    /// <summary>
    /// An IEEGDataSource which connects the the Emotiv headset
    /// </summary>
    public class EmotivDataSource : AbstractEEGDataSource
    {
        private static readonly EmotivDataSource instance = new EmotivDataSource();
        
        /// <summary>
        /// Singleton instance of this class.
        /// </summary>
        public static IEEGDataSource Instance { get { return instance; } }

        private readonly EmotivHeadset headset = EmotivHeadset.Instance;
        private int marker = EEGDataEntry.MARKER_DEFAULT, lastMarkerRead = EEGDataEntry.MARKER_DEFAULT, markerChangedTime = -1;

        /// <summary>
        /// Gets data from the headset
        /// </summary>
        protected override bool TryGetData(out IArrayView<EEGDataEntry> data)
        {
            Dictionary<EdkDll.EE_DataChannel_t, double[]> rawData;
            if (!this.headset.TryGetData(out rawData))
            {
                data = null;
                return false;
            }

            data = this.CreateEntries(rawData).ToIArray();
            return true;
        }

        private IEnumerable<EEGDataEntry> CreateEntries(Dictionary<EdkDll.EE_DataChannel_t, double[]> data)
        {
            // set the initial markerChangedTime
            if (this.markerChangedTime < 0 && data.TimeStamps().Length > 0)
                this.markerChangedTime = (int)(data.TimeStamps()[0] * 1000);

            for (int i = 0, length = data.TimeStamps().Length, timeStamp; i < length; i++)
            {
                timeStamp = (int)(data.TimeStamps()[i] * 1000);

                // if the current marker is different than the last one, update
                if (data[EdkDll.EE_DataChannel_t.MARKER][i] != EEGDataEntry.EMO_MARKER_DEFAULT)
                {
                    this.lastMarkerRead = (int)data[EdkDll.EE_DataChannel_t.MARKER][i];
                    this.markerChangedTime = timeStamp;
                }

                yield return new EEGDataEntry(this.lastMarkerRead, timeStamp, timeStamp - this.markerChangedTime, data.ChannelData(i));
            }
        }

        /// <summary>
        /// Sets the headset's marker or gets the last set marker
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
                    this.headset.SetMarker(this.marker);
                }
            }
        }

        /// <summary>
        /// The headset's lock is used as the synchronization root for this class
        /// </summary>
        protected override object Lock
        {
            get { return this.headset.Lock; }
        }

        /// <summary>
        /// Disposes the headset connection
        /// </summary>
        protected override void DisposeHelper()
        {
            this.headset.Dispose();
        }
    }
}
