namespace UnityStudio.Unity.MeshParts {
    public sealed class SubMesh {

        internal SubMesh() {
        }

        public uint FirstByte { get; internal set; }
        public uint IndexCount { get; internal set; }
        public int Topology { get; internal set; }
        public uint TriangleCount { get; internal set; }
        public uint FirstVertex { get; internal set; }
        public uint VertexCount { get; internal set; }
        public AABB BoundingBox { get; internal set; }

    }
}
