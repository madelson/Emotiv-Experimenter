using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    /// <summary>
    /// Simplifies the implementation of IEnumerable
    /// </summary>
    [Serializable]
    public abstract class AbstractEnumerable<T> : IEnumerable<T>
    {
        /// <summary>
        /// The enumerator implementation
        /// </summary>
        public abstract IEnumerator<T> GetEnumerator();

        /// <summary>
        /// The non-generic enumerable implementation
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Concatenates the enumerable's data into a string
        /// </summary>
        public override string ToString()
        {
            return string.Concat('[', this.ConcatToString(','), ']');
        }
    }
}
