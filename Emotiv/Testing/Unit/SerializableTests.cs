using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MCAEmotiv.Testing.Unit
{
    static class SerializableTests
    {
        public static void TestSerializable<T>(this T obj, Func<T, T, bool> equalsTest)
        {
            var formatter = new BinaryFormatter();
            byte[] serialized;
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, obj);
                serialized = stream.ToArray();
            }

            using (var stream = new MemoryStream(serialized))
            {
                var deserialized = (T)formatter.Deserialize(stream);
                if (!equalsTest(deserialized, obj))
                    throw new Exception(typeof(T).Name + " was not equal to the original object after serialization/deserialization!");
            }
        }
    }
}
