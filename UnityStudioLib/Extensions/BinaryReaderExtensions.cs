using System.IO;
using System.Text;
using JetBrains.Annotations;
using UnityStudio.Unity;

namespace UnityStudio.Extensions {
    public static class BinaryReaderExtensions {

        public static void AlignBy([NotNull] this BinaryReader reader, int alignment) {
            var position = reader.BaseStream.Position;
            var mod = position % alignment;
            if (mod != 0) {
                reader.BaseStream.Position += alignment - mod;
            }
        }

        [NotNull]
        public static string ReadAlignedString([NotNull] this BinaryReader reader) {
            var stringLength = reader.ReadInt32();
            var str = ReadAlignedString(reader, stringLength);

            return str;
        }

        [NotNull]
        public static string ReadAlignedString([NotNull] this BinaryReader reader, int length) {
            if (length <= 0 || length >= (reader.BaseStream.Length - reader.BaseStream.Position)) {
                return string.Empty;
            }

            var stringData = new byte[length];
            reader.Read(stringData, 0, length);
            var result = Encoding.UTF8.GetString(stringData);
            reader.AlignBy(4);
            return result;
        }

        public static Vector3 ReadVector3([NotNull] this BinaryReader reader) {
            var vector = new Vector3();

            vector.X = reader.ReadSingle();
            vector.Y = reader.ReadSingle();
            vector.Z = reader.ReadSingle();

            return vector;
        }

        public static Vector4 ReadVector4([NotNull] this BinaryReader reader) {
            var vector = new Vector4();

            vector.X = reader.ReadSingle();
            vector.Y = reader.ReadSingle();
            vector.Z = reader.ReadSingle();
            vector.W = reader.ReadSingle();

            return vector;
        }

        public static Quaternion ReadQuaternion([NotNull] this BinaryReader reader) {
            var quaternion = new Quaternion();

            quaternion.X = reader.ReadSingle();
            quaternion.Y = reader.ReadSingle();
            quaternion.Z = reader.ReadSingle();
            quaternion.W = reader.ReadSingle();

            return quaternion;
        }

    }
}
