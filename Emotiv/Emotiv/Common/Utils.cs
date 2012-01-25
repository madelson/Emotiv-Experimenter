using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace System
{
    /// <summary>
    /// Provides general-purpose utility methods
    /// </summary>
    public static class Utils
    {
        private static readonly Type[] emptyArgs = new Type[0];

        /// <summary>
        /// An empty array which can be passed as a parameter to invocation functions
        /// </summary>
        public static Type[] EmptyArgs { get { return emptyArgs; } }

        /// <summary>
        /// Returns true iff the file at path was successfully deserialized into an object
        /// of the requested type. Deserialized is set to the type's default value otherwise.
        /// </summary>
        public static bool TryDeserializeFile<T>(this string path, out T deserialized)
        {
            deserialized = default(T);
            if (!File.Exists(path))
                return false;

            try
            {
                using (var stream = File.OpenRead(path))
                    deserialized = (T)new BinaryFormatter().Deserialize(stream);
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
