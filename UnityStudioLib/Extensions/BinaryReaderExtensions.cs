using System.IO;
using System.Text;

namespace UnityStudio.Extensions {
    public static class BinaryReaderExtensions {

        public static void AlignBy(this BinaryReader reader, int alignment) {
            var position = reader.BaseStream.Position;
            var mod = position % alignment;
            if (mod != 0) {
                reader.BaseStream.Position += alignment - mod;
            }
        }

        public static string ReadAlignedString(this BinaryReader reader, int length) {
            if (length <= 0 || length >= (reader.BaseStream.Length - reader.BaseStream.Position)) {
                return string.Empty;
            }

            var stringData = new byte[length];
            reader.Read(stringData, 0, length);
            var result = Encoding.UTF8.GetString(stringData);
            reader.AlignBy(4);
            return result;
        }

    }
}
