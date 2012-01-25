using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.Transformations;

namespace MCAEmotiv.Analysis
{
    /// <summary>
    /// Determines the type of FFT output returned
    /// </summary>
    public enum FFTOutputType
    {
        /// <summary>
        /// Returns the real part of each complex output value
        /// </summary>
        Real,

        /// <summary>
        /// Returns the imaginary part of each complex output value
        /// </summary>
        Imaginary,

        /// <summary>
        /// Returns the magnitude of each complex output value
        /// </summary>
        Power,
    }

    /// <summary>
    /// Determines the window function used prior to computing the FFT
    /// </summary>
    public enum WindowType
    {
        /// <summary>
        /// A standard rectangular window
        /// </summary>
        Rectangular,

        /// <summary>
        /// The popular Hamming window
        /// </summary>
        Hamming,
    }

    /// <summary>
    /// Provides a convenient wrapper around the MathNet.Numerics.Transformation.RealFourierTransform class
    /// </summary>
    public class FFT
    {
        private double[] array;
        private IArray<double> iarray; 
        
        /// <summary>
        /// The number of elements used in the transform
        /// </summary>
        [Parameter("The number of elements used in the transform", MinValue = 0, DefaultValue = 64)]
        public int Length
        {
            get { return this.array.Length; }
            set 
            {
                int actual = value.NearestPowerOfTwo(false);
                if (this.array == null || this.array.Length != actual)
                {
                    this.array = new double[actual];
                    this.iarray = this.array.AsIArray();
                }
            }
        }

        /// <summary>
        /// The output style of the transform
        /// </summary>
        [Parameter("The output style of the transform", DefaultValue = FFTOutputType.Power)]
        public FFTOutputType OutputType { get; set; }

        /// <summary>
        /// The window function that is multiplied by the signal before the transformation
        /// </summary>
        [Parameter("The window function that is multiplied by the signal before the transformation", DefaultValue = WindowType.Rectangular)]
        public WindowType WindowType { get; set; }

        private readonly RealFourierTransformation fft;

        /// <summary>
        /// Construct an FFT with default parameter values
        /// </summary>
        public FFT() 
        { 
            this.fft = new RealFourierTransformation(TransformationConvention.Matlab);
            this.SetParametersToDefaultValues();
        }

        /// <summary>
        /// Transforms the signal based on the current settings. Returns the first half of the result,
        /// which is a reflection of the second half.
        /// </summary>
        public IArray<double> Transform(IEnumerable<double> signal)
        {
            this.iarray.Fill(this.WindowType.Apply(signal, this.Length).Take(this.Length));
            double[] real, imag;

            this.fft.TransformForward(this.array, out real, out imag);

            if (this.OutputType == FFTOutputType.Imaginary)
                return imag.AsIArray().SubArray(0, this.Length / 2);
            if (this.OutputType == FFTOutputType.Power)
                for (int i = 0; i < real.Length; i++)
                    real[i] = Math.Sqrt((real[i] * real[i]) + (imag[i] * imag[i]));
            return real.AsIArray().SubArray(0, this.Length / 2);
        }

        /// <summary>
        /// A static transformation method. The length is the nearest power of two that is not greater than the input length.
        /// Returns the full result.
        /// </summary>
        public static IArrayView<double> TransformRaw(IArrayView<double> input, FFTOutputType outputType = FFTOutputType.Power, WindowType windowType = WindowType.Rectangular)
        {
            int count = input.Count.NearestPowerOfTwo(false);
            
            var fft = new RealFourierTransformation();
            double[] window = windowType.Apply(input.Take(count), count).ToArray(), real, imag;
            fft.TransformForward(window, out real, out imag);

            if (outputType == FFTOutputType.Imaginary)
                return imag.AsIArray();
            if (outputType == FFTOutputType.Power)
                for (int i = 0; i < real.Length; i++)
                    real[i] = Math.Sqrt(real[i] * real[i] + imag[i] * imag[i]);

            return real.AsIArray();
        }
    }

    /// <summary>
    /// Contains useful FFT extension methods
    /// </summary>
    public static class FFTExtensions 
    {
        private static double ApplyRectangular(int index, int length) { return 1.0; }

        private static double ApplyHamming(int index, int length)
        {
            return 0.54 - (0.46 * Math.Cos((2.0 * Math.PI * index) / (length - 1))); 
        }

        /// <summary>
        /// Returns a function that generates the window function at any given the index and length
        /// </summary>
        public static Func<int, int, double> GetWindowFunction(this WindowType type)
        {
            switch (type)
            {
                case WindowType.Rectangular: return ApplyRectangular;
                case WindowType.Hamming: return ApplyHamming;
                default: throw new Exception("Unsupported window type");
            }
        }

        /// <summary>
        /// Returns the result of applying the window function to values using length
        /// </summary>
        public static IEnumerable<double> Apply(this WindowType type, IEnumerable<double> values, int length)
        {
            var func = type.GetWindowFunction();
            return values.Select((v, i) => v * func(i, length));
        }
    }
}
