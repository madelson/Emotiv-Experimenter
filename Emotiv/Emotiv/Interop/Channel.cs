using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emotiv;

namespace MCAEmotiv.Interop
{
    /// <summary>
    /// An enumeration of the Emotiv's channels
    /// </summary>
    [Serializable]
    [Description("A channel of data from one of the Emotiv's electrodes")]
    public enum Channel
    {
        /// <summary>
        /// Left frontmost 
        /// </summary>
        [Description("Left frontmost")]
        AF3 = EdkDll.EE_DataChannel_t.AF3,

        /// <summary>
        /// Leftmost frontal
        /// </summary>
        [Description("Leftmost frontal")]
        F7 = EdkDll.EE_DataChannel_t.F7,

        /// <summary>
        /// Left frontal
        /// </summary>
        [Description("Left frontal")]
        F3 = EdkDll.EE_DataChannel_t.F3,

        /// <summary>
        /// Left frontal-central
        /// </summary>
        [Description("Left frontal-central")]
        FC5 = EdkDll.EE_DataChannel_t.FC5,

        /// <summary>
        /// Left temporal
        /// </summary>
        [Description("Left temporal")]
        T7 = EdkDll.EE_DataChannel_t.T7,

        /// <summary>
        /// Left parietal
        /// </summary>
        [Description("Left parietal")]
        P7 = EdkDll.EE_DataChannel_t.P7,

        /// <summary>
        /// Left occipital
        /// </summary>
        [Description("Left occipital")]
        O1 = EdkDll.EE_DataChannel_t.O1,

        /// <summary>
        /// Right occipital
        /// </summary>
        [Description("Right occipital")]
        O2 = EdkDll.EE_DataChannel_t.O2,

        /// <summary>
        /// Right parietal
        /// </summary>
        [Description("Right parietal")]
        P8 = EdkDll.EE_DataChannel_t.P8,

        /// <summary>
        /// Right temporal
        /// </summary>
        [Description("Right temporal")]
        T8 = EdkDll.EE_DataChannel_t.T8,

        /// <summary>
        /// Right frontal-central
        /// </summary>
        [Description("Right frontal-central")]
        FC6 = EdkDll.EE_DataChannel_t.FC6,

        /// <summary>
        /// Right frontal
        /// </summary>
        [Description("Right frontal")]
        F4 = EdkDll.EE_DataChannel_t.F4,

        /// <summary>
        /// Rightmost frontal
        /// </summary>
        [Description("Rightmost frontal")]
        F8 = EdkDll.EE_DataChannel_t.F8,

        /// <summary>
        /// Right frontmost
        /// </summary>
        [Description("Right frontmost")]
        AF4 = EdkDll.EE_DataChannel_t.AF4,
    }

    /// <summary>
    /// Utilities and extensions for the Channel enumeration
    /// </summary>
    public static class Channels {
        private static readonly IArrayView<Channel> values;
        private static readonly Dictionary<Channel, int> channelsToIndices;

        /// <summary>
        /// An array containing all Channel values
        /// </summary>
        public static IArrayView<Channel> Values { get { return values; } }

        static Channels()
        {
            var chanValues = Enum.GetValues(typeof(Channel));
            var chanValuesArray = new Channel[chanValues.Length];
            chanValues.CopyTo(chanValuesArray, 0);

            values = chanValuesArray.ToIArray();
            channelsToIndices = values
                .ParallelTo(values.Indices())
                .ToDictionary(d => d.Item1, d => d.Item2);
        }

        /// <summary>
        /// Converts the channel to an EdkDll.EE_DataChannel_t enumeration
        /// </summary>
        public static EdkDll.EE_DataChannel_t ToEdkChannel(this Channel channel) { return (EdkDll.EE_DataChannel_t)channel; }
        
        /// <summary>
        /// Returns the index of the channel in Channels.Values
        /// </summary>
        public static int ToIndex(this Channel channel) { return channelsToIndices[channel]; }

        /// <summary>
        /// Returns the channel which is in the same location as this channel, but on a mirror
        /// image of half of the head
        /// </summary>
        public static Channel Mirror(this Channel channel)
        {
            switch (channel)
            {
                case Channel.AF3: return Channel.AF4;
                case Channel.AF4: return Channel.AF3;
                case Channel.F7: return Channel.F8;
                case Channel.F8: return Channel.F7;
                case Channel.F3: return Channel.F4;
                case Channel.F4: return Channel.F3;
                case Channel.FC5: return Channel.FC6;
                case Channel.FC6: return Channel.FC5;
                case Channel.T7: return Channel.T8;
                case Channel.T8: return Channel.T7;
                case Channel.P7: return Channel.P8;
                case Channel.P8: return Channel.P7;
                case Channel.O1: return Channel.O2;
                case Channel.O2: return Channel.O1;
                default: throw new Exception("Not a channel");
            }
        }

        /// <summary>
        /// Returns the set of channel values at the requested index
        /// </summary>
        public static IEnumerable<double> ChannelData(this Dictionary<EdkDll.EE_DataChannel_t, double[]> data, int index)
        {
            foreach (var channel in Values)
                yield return data[channel.ToEdkChannel()][index];
        }

        /// <summary>
        /// Returns the sequence of timestamp values
        /// </summary>
        public static double[] TimeStamps(this Dictionary<EdkDll.EE_DataChannel_t, double[]> data)
        {
            return data[EdkDll.EE_DataChannel_t.TIMESTAMP];
        }
    }
}
