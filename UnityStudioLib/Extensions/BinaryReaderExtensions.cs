using System.IO;
using System.Text;
using JetBrains.Annotations;
using UnityStudio.UnityEngine;
using UnityStudio.UnityEngine.MeshParts;

namespace UnityStudio.Extensions {
    internal static class BinaryReaderExtensions {

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

        internal static Vector2 ReadVector2([NotNull] this BinaryReader reader) {
            var vector = new Vector2();

            vector.X = reader.ReadSingle();
            vector.Y = reader.ReadSingle();

            return vector;
        }

        internal static Vector3 ReadVector3([NotNull] this BinaryReader reader) {
            var vector = new Vector3();

            vector.X = reader.ReadSingle();
            vector.Y = reader.ReadSingle();
            vector.Z = reader.ReadSingle();

            return vector;
        }

        internal static Vector4 ReadVector4([NotNull] this BinaryReader reader) {
            var vector = new Vector4();

            vector.X = reader.ReadSingle();
            vector.Y = reader.ReadSingle();
            vector.Z = reader.ReadSingle();
            vector.W = reader.ReadSingle();

            return vector;
        }

        internal static Quaternion ReadQuaternion([NotNull] this BinaryReader reader) {
            var quaternion = new Quaternion();

            quaternion.X = reader.ReadSingle();
            quaternion.Y = reader.ReadSingle();
            quaternion.Z = reader.ReadSingle();
            quaternion.W = reader.ReadSingle();

            return quaternion;
        }

        [NotNull]
        internal static BlendShapeData ReadBlendShapeData([NotNull] this BinaryReader reader) {
            var vertexCount = reader.ReadInt32();
            var vertices = new BlendShapeVertex[vertexCount];

            for (var i = 0; i < vertexCount; ++i) {
                vertices[i] = ReadBlendShapeVertex(reader);
            }

            var shapeCount = reader.ReadInt32();
            var shapes = new MeshBlendShape[shapeCount];

            for (var i = 0; i < shapeCount; ++i) {
                shapes[i] = ReadMeshBlendShape(reader);
            }

            var channelCount = reader.ReadInt32();
            var channels = new MeshBlendShapeChannel[channelCount];

            for (var i = 0; i < channelCount; ++i) {
                channels[i] = ReadMeshBlendShapeChannel(reader);
            }

            var weightCount = reader.ReadInt32();
            var weights = new float[weightCount];

            for (var i = 0; i < weightCount; ++i) {
                weights[i] = reader.ReadSingle();
            }

            return new BlendShapeData(vertices, shapes, channels, weights);

            BlendShapeVertex ReadBlendShapeVertex(BinaryReader r) {
                var vertex = new BlendShapeVertex();

                vertex.Vertex = r.ReadVector3();
                vertex.Normal = r.ReadVector3();
                vertex.Tangent = r.ReadVector3();
                vertex.Index = r.ReadUInt32();

                return vertex;
            }

            MeshBlendShape ReadMeshBlendShape(BinaryReader r) {
                var shape = new MeshBlendShape();

                shape.FirstVertex = r.ReadUInt32();
                shape.VertexCount = r.ReadUInt32();
                shape.HasNormals = r.ReadBoolean();
                shape.HasTangents = r.ReadBoolean();

                return shape;
            }

            MeshBlendShapeChannel ReadMeshBlendShapeChannel(BinaryReader r) {
                var channel = new MeshBlendShapeChannel();

                channel.Name = r.ReadAlignedString();
                channel.NameHash = r.ReadUInt32();
                channel.FrameIndex = r.ReadInt32();
                channel.FrameCount = r.ReadInt32();

                return channel;
            }
        }


    }
}
