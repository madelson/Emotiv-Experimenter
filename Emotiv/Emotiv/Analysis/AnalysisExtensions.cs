using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.Interop;

namespace MCAEmotiv.Analysis
{
    /// <summary>
    /// Provides extensions related to data analysis?
    /// </summary>
    public static class AnalysisExtensions
    {
        /// <summary>
        /// Implements an artifact-detection algorithm
        /// </summary>
        public static bool HasMotionArtifact(this IEnumerable<EEGDataEntry> trial,
            double round1Alpha = 0.025,
            double round1Threshold = 40.0,
            double round2Alpha = 0.5,
            double round2Threshold = 40.0,
            Channel channel = Channel.AF3,
            bool useMirror = false)
        {
            var channelData = trial.Channel(channel);
            double avg = channelData.Average();
            if (channelData.MovingAverages(round1Alpha).Select(ma => Math.Abs(ma - avg)).Max() > round1Threshold)
                return true;
            if (channelData.ParallelTo(channelData.MovingAverages(round1Alpha)).Select(d => d.Item1 - d.Item2).MovingAverages(round2Alpha).Max() > round2Threshold)
                return true;
            return useMirror
                ? trial.HasMotionArtifact(round1Alpha, round1Threshold, round2Alpha, round2Threshold, channel.Mirror(), false)
                : false;
        }

        private const int MAX_POWER = 1 << 30;

        /// <summary>
        /// Returns the power of two nearest to value
        /// </summary>
        public static int NearestPowerOfTwo(this int value, bool canReturnGreaterValues = true)
        {
            if (value >= MAX_POWER)
                return MAX_POWER;

            int power = 1;
            while (power <= value)
                power <<= 1;

            if (canReturnGreaterValues)
            {
                return power - value > value - (power >> 1)
                    ? power >> 1
                    : power;
            }
            else
                return power >> 1;
        }

        /// <summary>
        /// Averages together adjacent bins in values
        /// </summary>
        public static IArrayView<double> Downsample(this IArrayView<double> values, int binWidth)
        {
            if (binWidth <= 0)
                throw new ArgumentException("binWidth must be positive");

            int div = values.Count / binWidth, mod = values.Count % binWidth;
            double[] downsampled = new double[div + Math.Sign(mod)];
            int offset;
            for (int i = 0; i < div; i++)
            {
                offset = binWidth * i;
                for (int j = 0; j < binWidth; j++)
                    downsampled[i] += values[offset + j];
                downsampled[i] /= binWidth;
            }
            if (div < downsampled.Length)
                downsampled[div] = values.SubView(values.Count - mod, mod).Average();

            return downsampled.AsIArray();
        }

        /// <summary>
        /// Computes the average of each "feature"
        /// </summary>
        public static IArray<double> Averages(this IEnumerable<IEnumerable<double>> data)
        {
            var sums = new List<double>(data.First());
            int count = 1, i;
            foreach (var enumerable in data.Skip(1))
            {
                count++;
                i = 0;
                foreach (double d in enumerable)
                    if (sums.Count <= i)
                        break;
                    else
                        sums[i++] += d;
                for (int j = sums.Count - 1; j >= i; j--)
                    sums.RemoveAt(j);
            }

            for (i = 0; i < sums.Count; i++)
                sums[i] /= count;

            return sums.AsIArray();
        }

        /// <summary>
        /// Computes the mean and standard deviation of each feature.
        /// </summary>
        public static IArrayView<double> StandardDevations(this IEnumerable<IArrayView<double>> examples, out IArrayView<double> means)
        {
            double[] stddevsArray = new double[examples.First().Count], meansArray = new double[stddevsArray.Length];
            double mean;
            for (int i = 0; i < stddevsArray.Length; i++)
            {
                stddevsArray[i] = examples.Select(e => e[i]).StandardDeviation(out mean);
                meansArray[i] = mean;
            }

            means = meansArray.AsIArray();
            return stddevsArray.AsIArray();
        }

        /// <summary>
        /// ZScores each feature based on stddevs and means.
        /// </summary>
        public static IArrayView<IArrayView<double>> ZScored(this IArrayView<IArrayView<double>> examples, IArrayView<double> stddevs, IArrayView<double> means)
        {
            return examples.SelectArray(e => e.SelectArray((d, i) => (d - means[i]) / stddevs[i]));
        }
    }
}
