using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityStudio.Extensions {
    public static class StreamExtensions {

        public static string ReadStringToNull(this Stream stream) {
            return ReadStringToNull(stream, Encoding.UTF8);
        }

        public static string ReadStringToNull(this Stream stream, Encoding encoding) {
            var bytes = new List<byte>();
            var b = stream.ReadByte();
            while (b != 0) {
                bytes.Add(unchecked((byte)b));
                b = stream.ReadByte();
            }
            return encoding.GetString(bytes.ToArray());
        }

        public static string ReadStringToNull(this BinaryReader reader) {
            return ReadStringToNull(reader, Encoding.UTF8);
        }

        public static string ReadStringToNull(this BinaryReader reader, Encoding encoding) {
            return reader.BaseStream.ReadStringToNull(encoding);
        }

    }
}
