using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emotiv;
using System.Threading;

namespace MCAEmotiv.Interop
{
    /// <summary>
    /// Acts as a wrapper for the lower-level EmoEngine class.
    /// </summary>
    internal class EmotivHeadset : SafeDisposable
    {
        private const int BUF_SIZE_SECONDS = 1, NO_USER = -1;

        private static readonly EmotivHeadset instance = new EmotivHeadset();
        
        /// <summary>
        /// Singleton instance of this class
        /// </summary>
        public static EmotivHeadset Instance { get { return instance; } }
        private static readonly object myLock = new object();
        public object Lock { get { return myLock; } }

        private readonly EmoEngine engine = EmoEngine.Instance;
        private volatile int userID = NO_USER, lastMarker = EEGDataEntry.MARKER_DEFAULT;

        private EmotivHeadset()
        {
            this.engine.UserAdded += (sender, args) =>
            {
                lock (this.Lock)
                {
                    // record the user
                    this.userID = (int)args.userId;

                    // enable data aquisition for this user.
                    this.engine.DataAcquisitionEnable((uint)this.userID, true);

                    // set buffer size
                    this.engine.EE_DataSetBufferSizeInSec(BUF_SIZE_SECONDS);

                    // set marker
                    this.engine.DataSetMarker((uint)this.userID, this.lastMarker);
                }
            };

            this.engine.UserRemoved += (sender, args) =>
            {
                lock (this.Lock)
                    this.userID = NO_USER;
            };

            this.engine.Connect();
        }

        /// <summary>
        /// Returns true upon successfully retrieving data from the headset, and false otherwise
        /// </summary>
        public bool TryGetData(out Dictionary<EdkDll.EE_DataChannel_t, double[]> data)
        {
            data = null;
            lock (this.Lock)
            {
                // if no user, we can't succeed
                if (this.userID == NO_USER)
                    this.engine.ProcessEvents();

                try
                {
                    data = this.engine.GetData((uint)this.userID);
                    if (data == null)
                    {
                        Thread.Sleep(10);
                        data = this.engine.GetData((uint)this.userID);
                    }
                }
                catch (Exception) { }

                // error case
                if (data == null)
                {
                    //this.userID = NO_USER;
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Sets the headset's marker
        /// </summary>
        public void SetMarker(int marker)
        {
            lock (this.Lock)
                this.engine.DataSetMarker((uint)this.userID, (this.lastMarker = marker));
        }

        protected override void DisposeOfManagedResources()
        {
            this.engine.Disconnect();
        }
    }
}
