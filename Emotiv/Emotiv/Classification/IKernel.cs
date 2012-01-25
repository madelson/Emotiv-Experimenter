using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.Classification
{
    /// <summary>
    /// A kernel function that operates on an inner product
    /// </summary>
    public interface IKernel
    {
        /// <summary>
        /// Evaluates the kernel wrt the inner product
        /// </summary>
        double Evaluate(double innerProduct);
    }

    #region ---- Implementations ----
    /// <summary>
    /// Kernel computes (a . b)
    /// </summary>
    [Serializable]
    [Description("Kernel computes (a . b)", DisplayName = "Basic")]
    public sealed class BasicKernel : IKernel
    {
        /// <summary>
        /// Kernel computes (a . b)
        /// </summary>
        public double Evaluate(double innerProduct)
        {
            return innerProduct;
        }

        /// <summary>
        /// True if obj is also a basic kernel
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is BasicKernel;
        }

        /// <summary>
        /// A hash code method for BasicKernel
        /// </summary>
        public override int GetHashCode()
        {
            return typeof(BasicKernel).GetHashCode();
        }
    }

    /// <summary>
    /// Kernel computes ((Scale * (a . b)) + Offset) ^ Degree
    /// </summary>
    [Serializable]
    [Description(DESCRIPTION, DisplayName = "Polynomial")]
    public class PolynomialKernel : IKernel
    {
        private const string DESCRIPTION = "Kernel computes ((Scale * (a . b)) + Offset) ^ Degree";

        /// <summary>
        /// Kernel computes ((Scale * (a . b)) + Offset) ^ Degree
        /// </summary>
        [Parameter(DESCRIPTION, DefaultValue = 3.0, MinValue = 1.0)]
        public double Degree { get; set; }

        /// <summary>
        /// Kernel computes ((Scale * (a . b)) + Offset) ^ Degree
        /// </summary>
        [Parameter(DESCRIPTION, DefaultValue = 1.0)]
        public double Scale { get; set; }

        /// <summary>
        /// Kernel computes ((Scale * (a . b)) + Offset) ^ Degree
        /// </summary>
        [Parameter(DESCRIPTION, DefaultValue = 1.0)]
        public double Offset { get; set; }

        /// <summary>
        /// Construct a Polynomial Kernel with default parameters
        /// </summary>
        public PolynomialKernel() { this.SetParametersToDefaultValues(); }

        /// <summary>
        /// Kernel computes ((Scale * (a . b)) + Offset) ^ Degree
        /// </summary>
        public double Evaluate(double innerProduct)
        {
            return Math.Pow((this.Scale * innerProduct) + this.Offset, this.Degree);
        }
    }

    /// <summary>
    /// "Kernel computes tanh((Scale * (a . b)) + Offset)"
    /// </summary>
    [Serializable]
    [Description(DESCRIPTION, DisplayName = "Hyperbolic Tangent")]
    public class HyperbolicTangentKernel : IKernel
    {
        private const string DESCRIPTION = "Kernel computes tanh((Scale * (a . b)) + Offset)";

        /// <summary>
        /// "Kernel computes tanh((Scale * (a . b)) + Offset)"
        /// </summary>
        [Parameter(DESCRIPTION, DefaultValue = 1.0)]
        public double Scale { get; set; }

        /// <summary>
        /// "Kernel computes tanh((Scale * (a . b)) + Offset)"
        /// </summary>
        [Parameter(DESCRIPTION, DefaultValue = 1.0)]
        public double Offset { get; set; }

        /// <summary>
        /// Creates a HyperbolicTangentKernel with default parameters
        /// </summary>
        public HyperbolicTangentKernel() { this.SetParametersToDefaultValues(); }

        /// <summary>
        /// "Kernel computes tanh((Scale * (a . b)) + Offset)"
        /// </summary>
        public double Evaluate(double innerProduct)
        {
            return Math.Tanh((this.Scale * innerProduct) + this.Offset);
        }
    }
    #endregion

    /// <summary>
    /// Utility and extension methods for kernels
    /// </summary>
    public static class Kernels
    {
        /// <summary>
        /// Evaluates the kernel function on a and b
        /// </summary>
        public static double Evaluate(this IKernel kernel, IEnumerable<double> a, IEnumerable<double> b)
        {
            return kernel.Evaluate(a.InnerProduct(b));
        }
    }
}
