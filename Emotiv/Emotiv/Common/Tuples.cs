using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    /// <summary>
    /// An interface for a 2-tuple
    /// </summary>
    public interface IDuo<out I1, out I2>
    {
        /// <summary>
        /// The first item in the tuple
        /// </summary>
        I1 Item1 { get; }

        /// <summary>
        /// The second item in the tuple
        /// </summary>
        I2 Item2 { get; }
    }

    /// <summary>
    /// An interface for a 3-tuple
    /// </summary>
    public interface ITrio<out I1, out I2, out I3> : IDuo<I1, I2>
    {
        /// <summary>
        /// The third item in the tuple
        /// </summary>
        I3 Item3 { get; }
    }

    /// <summary>
    /// An immutable 2-tuple
    /// </summary>
    [Serializable]
    public struct Duo<I1, I2> : IDuo<I1, I2>, IEquatable<Duo<I1, I2>>
    {
        private readonly I1 item1;
        private readonly I2 item2;

        /// <summary>
        /// The first item in the tuple
        /// </summary>
        public I1 Item1 { get { return this.item1; } }

        /// <summary>
        /// The second item in the tuple
        /// </summary>
        public I2 Item2 { get { return this.item2; } }

        /// <summary>
        /// Construct a tuple with the given items
        /// </summary>
        public Duo(I1 item1, I2 item2)
        {
            this.item1 = item1;
            this.item2 = item2;
        }

        /// <summary>
        /// A coordinate pair string for this tuple
        /// </summary>
        public override string ToString()
        {
            return string.Format("({0}, {1})", this.Item1, this.Item2);
        }

        /// <summary>
        /// Item-wise equality check
        /// </summary>
        public bool Equals(Duo<I1, I2> other)
        {
            return this.Item1.Equals(other.Item1) && this.Item2.Equals(other.Item2);
        }

        /// <summary>
        /// Returns a hash code based on the constituent items
        /// </summary>
        public override int GetHashCode()
        {
            return unchecked(this.Item1.GetHashCode() * this.Item2.GetHashCode());
        }

        /// <summary>
        /// Item-wise equality check
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Duo<I1, I2> && this.Equals((Duo<I1, I2>)obj);
        }
    }

    /// <summary>
    /// An immutable 3-tuple
    /// </summary>
    [Serializable]
    public struct Trio<I1, I2, I3> : ITrio<I1, I2, I3>, IEquatable<Trio<I1, I2, I3>>
    {
        private readonly I1 item1;
        private readonly I2 item2;
        private readonly I3 item3;

        /// <summary>
        /// The first item in the tuple
        /// </summary>
        public I1 Item1 { get { return this.item1; } }

        /// <summary>
        /// The second item in the tuple
        /// </summary>
        public I2 Item2 { get { return this.item2; } }

        /// <summary>
        /// The third item in the tuple
        /// </summary>
        public I3 Item3 { get { return this.item3; } }

        /// <summary>
        /// Construct a 3-tuple from the items
        /// </summary>
        public Trio(I1 item1, I2 item2, I3 item3)
        {
            this.item1 = item1;
            this.item2 = item2;
            this.item3 = item3;
        }

        /// <summary>
        /// A coordinate pair-based string representation
        /// </summary>
        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", this.Item1, this.Item2, this.Item3);
        }

        /// <summary>
        /// Checks item-wise equality
        /// </summary>
        public bool Equals(Trio<I1, I2, I3> other)
        {
            return this.Item1.Equals(other.Item1) && this.Item2.Equals(other.Item2) && this.Item3.Equals(other.Item3);
        }

        /// <summary>
        /// Returns an item-based hash code
        /// </summary>
        public override int GetHashCode()
        {
            return unchecked(this.Item1.GetHashCode() * this.Item2.GetHashCode() * this.Item3.GetHashCode());
        }

        /// <summary>
        /// Checks item-wise equality
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Trio<I1, I2, I3> && this.Equals((Trio<I1, I2, I3>)obj);
        }
    }

    /// <summary>
    /// Provides utility and extension methods for tuples
    /// </summary>
    public static class Tuples
    {
        /// <summary>
        /// Creates a new trio
        /// </summary>
        public static Duo<I1, I2> New<I1, I2>(I1 item2, I2 item3)
        {
            return new Duo<I1, I2>(item2, item3);
        }

        /// <summary>
        /// Creates a new tuple
        /// </summary>
        public static Trio<I1, I2, I3> New<I1, I2, I3>(I1 item1, I2 item2, I3 item3)
        {
            return new Trio<I1, I2, I3>(item1, item2, item3);
        }

        /// <summary>
        /// Reverses the Duo tuple
        /// </summary>
        public static Duo<I2, I1> Reversed<I1, I2>(this Duo<I1, I2> duo)
        {
            return New(duo.Item2, duo.Item1);
        }
    }
}
