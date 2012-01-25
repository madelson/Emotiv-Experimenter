using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.GUI
{
    /// <summary>
    /// A class which can be used to wrap an object in a checked list box
    /// </summary>
    public class DisplayPointer
    {
        /// <summary>
        /// Retrieves an object whose ToString method is used as the display pointer's
        /// ToString method
        /// </summary>
        public Func<object> Getter { get; private set; }

        /// <summary>
        /// May be used to associate another object with this instance
        /// </summary>
        public object Key { get; private set; }

        /// <summary>
        /// Construct a pointer with the specified getter and key
        /// </summary>
        public DisplayPointer(Func<object> getter, object key = null)
        {
            this.Getter = getter;
            this.Key = key;
        }

        /// <summary>
        /// Construct a display pointer with a static display string and the
        /// specified key
        /// </summary>
        public DisplayPointer(string staticDisplayString, object key = null)
            : this(() => staticDisplayString, key)
        {
        }

        /// <summary>
        /// Forwards the call to the object returned by the getter
        /// </summary>
        public override string ToString()
        {
            return this.Getter().ToString();
        }
    }
}
