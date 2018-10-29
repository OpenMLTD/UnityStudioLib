namespace UnityStudio.UnityEngine.MeshParts {
    public sealed class MeshBlendShape {

        internal MeshBlendShape() {
        }

        public uint FirstVertex { get; internal set; }
        public uint VertexCount { get; internal set; }
        public bool HasNormals { get; internal set; }
        public bool HasTangents { get; internal set; }

    }
}
