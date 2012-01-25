using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MCAEmotiv.Interop
{
    /// <summary>
    /// Contains one EEG data point, with marker, timestamp, and voltage data. This class is immutable.
    /// </summary>
    [Serializable]
    public struct EEGDataEntry
    {
        /// <summary>
        /// The separator character used in ToString
        /// </summary>
        public const char SEPARATOR = ',';

        /// <summary>
        /// The number of fields in an entry's ToString output
        /// </summary>
        public static readonly int NUM_LINE_FIELDS = Channels.Values.Count + 3;

        /// <summary>
        /// A value used to mark the presentation of an unknown stimulus
        /// <value>-2</value>
        /// </summary>
        public const int MARKER_UNKNOWN = -2;

        /// <summary>
        /// A value used to mark the absence of a stimulus
        /// <value>-1</value>
        /// </summary>
        public const int MARKER_DEFAULT = -1;

        /// <summary>
        /// The default marker value returned by the headset
        /// <value>0</value>
        /// </summary>
        public const int EMO_MARKER_DEFAULT = 0;

        /// <summary>
        /// Provides an indication of the current trial.
        /// </summary>
        public int Marker { get; private set; }

        /// <summary>
        /// Time in millis.
        /// </summary>
        public int TimeStamp { get; private set; }

        /// <summary>
        /// Time relative to stimulus onset.
        /// </summary>
        public int RelativeTimeStamp { get; private set; }

        /// <summary>
        /// The voltage data at each channel
        /// </summary>
        public IArrayView<double> Data { get; private set; }

        /// <summary>
        /// Provides explicit channel-based access to data
        /// </summary>
        public double this[Channel channel] { get { return this.Data[channel.ToIndex()]; } }

        /// <summary>
        /// Constructs an EEGDataEntry. A copy of channelData is created.
        /// </summary>
        public EEGDataEntry(int marker, int timeStamp, int relativeTimeStamp, IEnumerable<double> channelData)
            : this(marker, timeStamp, relativeTimeStamp, channelData.ToIArray())
        {
            foreach (var d in this.Data)
                if (double.IsNaN(d) || double.IsInfinity(d))
                    throw new ArgumentOutOfRangeException("channelData", channelData.ConcatToString(","),
                        "Does not accept NaN or Inf values!");
        }

        private EEGDataEntry(int marker, int timeStamp, int relativeTimeStamp, IArrayView<double> channelData)
            : this()
        {
            this.Data = channelData;
            this.Marker = marker;
            this.TimeStamp = timeStamp;
            this.RelativeTimeStamp = relativeTimeStamp;
        }

        /// <summary>
        /// Create an identical entry except for the marker field. Re-uses the channel data
        /// </summary>
        public EEGDataEntry WithMarker(int marker)
        {
            return new EEGDataEntry(marker, this.TimeStamp, this.RelativeTimeStamp, this.Data);
        }

        /// <summary>
        /// A CSV string representation which can be parsed by Parse.
        /// </summary>
        public override string ToString()
        {
            return ((object)this.Marker)
                .Then(this.TimeStamp)
                .Then(this.RelativeTimeStamp)
                .Then(this.Data.ConcatToString(SEPARATOR, "0.00"))
                .ConcatToString(SEPARATOR);
        }

        /// <summary>
        /// Checks all fields to determine equality
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is EEGDataEntry))
                return false;

            var that = (EEGDataEntry)obj;
            return this.TimeStamp == that.TimeStamp
                && this.RelativeTimeStamp == that.RelativeTimeStamp
                && this.Data.SequenceEqual(that.Data)
                && this.Marker == that.Marker;
        }

        /// <summary>
        /// Returns a hashcode for this entry
        /// </summary>
        public override int GetHashCode()
        {
            return unchecked(this.Marker * this.TimeStamp * this.RelativeTimeStamp);
        }

        /// <summary>
        /// Parses a line of text as an EEGDataEntry. The line should have been created with ToString
        /// </summary>
        public static EEGDataEntry Parse(string line)
        {
            var fields = line.Split(SEPARATOR);

            return new EEGDataEntry(int.Parse(fields[0]),
                int.Parse(fields[1]),
                int.Parse(fields[2]),
                fields.Skip(3).Select(double.Parse));
        }

        /// <summary>
        /// Parses a line of text based on a format that is essentially the raw output of the emotiv headset
        /// </summary>
        public static EEGDataEntry ParseOldFormat(string line, int onsetTime)
        {
            var fields = line.Split(SEPARATOR);
            int time = (1000 * double.Parse(fields[Channels.Values.Count + 4])).Rounded();
            return new EEGDataEntry(int.Parse(fields[0]),
                time,
                -1,
                fields.Skip(2).Take(Channels.Values.Count).Select(double.Parse));
        }

        /// <summary>
        /// Attempts to use Parse/ParseOldFormat to extract the entries in the file
        /// </summary>
        public static IEnumerable<EEGDataEntry> FromFile(string path)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0)
                return Enumerable.Empty<EEGDataEntry>();

            if (lines[0].Split(SEPARATOR).Length <= NUM_LINE_FIELDS)
                return lines.Select(Parse);
            else
            {
                var parsed = new List<EEGDataEntry>(lines.Length) { ParseOldFormat(lines[0], 0) };
                int onsetTime = 0;
                string marker = parsed[0].Marker.ToString();
                foreach (var line in lines.Skip(1))
                {
                    if (!line.StartsWith(marker))
                    {
                        onsetTime = 0;
                        marker = line.Substring(0, line.IndexOf(','));
                    }
                    parsed.Add(ParseOldFormat(line, onsetTime));
                }

                return parsed;
            }
        }
    }

    /// <summary>
    /// Extension methods for EEGDataEntry and collections of entries
    /// </summary>
    public static class EEGDataEntryExtensions
    {
        /// <summary>
        /// Was a stimulus showing when this entry was recorded?
        /// </summary>
        public static bool HasStimulusMarker(this EEGDataEntry entry)
        {
            return entry.Marker != EEGDataEntry.MARKER_DEFAULT && entry.Marker != EEGDataEntry.EMO_MARKER_DEFAULT;
        }

        /// <summary>
        /// Groups entries using the sequences formed by their relative timestamps
        /// </summary>
        public static IEnumerable<IArrayView<EEGDataEntry>> SectionByStimulus(this IEnumerable<EEGDataEntry> entries)
        {
            if (entries.IsEmpty())
                yield break;

            var section = new List<EEGDataEntry>() { entries.First() };
            foreach (var entry in entries.Skip(1))
            {
                if (entry.RelativeTimeStamp >= section.LastItem().RelativeTimeStamp)
                    section.Add(entry);
                else
                {
                    yield return section.AsIArray();
                    section = new List<EEGDataEntry>() { entry };
                }
            }

            yield return section.AsIArray(); // return the last section
        }

        /// <summary>
        /// Returns each entry's voltage value at the desired channel index
        /// </summary>
        public static IEnumerable<double> Channel(this IEnumerable<EEGDataEntry> entries, int channel)
        {
            foreach (var entry in entries)
                yield return entry.Data[channel];
        }

        /// <summary>
        /// Returns each entry's voltage value at the desired channel
        /// </summary>
        public static IEnumerable<double> Channel(this IEnumerable<EEGDataEntry> entries, Channel channel)
        {
            foreach (var entry in entries)
                yield return entry[channel];
        }

        /// <summary>
        /// Returns the channel time series represented by the series of entries
        /// </summary>
        public static IEnumerable<IEnumerable<double>> Channels(this IEnumerable<EEGDataEntry> entries)
        {
            return MCAEmotiv.Interop.Channels.Values.Select(ch => entries.Channel(ch));
        }

        /// <summary>
        /// Averages adjacent entries such that the result has only one entry per time bin of the specified width
        /// </summary>
        public static IArrayView<EEGDataEntry> DownSample(this IEnumerable<EEGDataEntry> entries, int binWidthMillis)
        {
            var bins = entries.BinByTime(binWidthMillis);
            var downSampled = Arrays.New<EEGDataEntry>(bins.Count);
            for (int i = 0; i < bins.Count; i++)
            {
                if (bins[i].Count == 0)
                {
                    var baseEntry = downSampled[i - 1];
                    downSampled[i] = new EEGDataEntry(baseEntry.Marker, baseEntry.TimeStamp + binWidthMillis, baseEntry.RelativeTimeStamp + binWidthMillis, baseEntry.Data);
                }
                else
                    downSampled[i] = bins[i].AverageEntry();
            }

            return downSampled;
        }

        /// <summary>
        /// Groups the entries by their respective time bins
        /// </summary>
        public static IArrayView<IArrayView<EEGDataEntry>> BinByTime(this IEnumerable<EEGDataEntry> entries, int binWidthMillis)
        {
            if (binWidthMillis <= 0)
                throw new ArgumentOutOfRangeException("binWidthMillis");

            int baseTime = entries.First().TimeStamp;
            var bins = new List<IArrayView<EEGDataEntry>>();
            var bin = new List<EEGDataEntry>();
            foreach (var entry in entries)
                // add it the the current bin
                if (entry.TimeStamp < baseTime + binWidthMillis)
                    bin.Add(entry);
                else // save the old bin and add it to the new one
                {
                    bins.Add(bin.AsIArray());

                    while (true)
                    {
                        baseTime += binWidthMillis;
                        if (entry.TimeStamp < baseTime + binWidthMillis)
                        {
                            bin = new List<EEGDataEntry>() { entry };
                            break;
                        }

                        bins.Add(Arrays.New<EEGDataEntry>(0)); // empty array
                    }
                }

            // make sure to get the last bin!
            if (bin.Count > 0)
                bins.Add(bin.AsIArray());

            return bins.AsIArray();
        }

        /// <summary>
        /// Averages a collection of entries together
        /// </summary>
        public static EEGDataEntry AverageEntry(this IEnumerable<EEGDataEntry> entries)
        {
            var first = entries.First();
            var channels = new double[first.Data.Count];
            int marker = first.Marker, count = 0;
            foreach (var entry in entries)
            {
                for (int i = 0; i < channels.Length; i++)
                    channels[i] += entry.Data[i];
                count++;
            }

            return new EEGDataEntry(marker, first.TimeStamp, first.RelativeTimeStamp, channels.Select(d => d / count));
        }
    }
}
