namespace UnityStudio.UnityEngine.MeshParts {
    public sealed class SubMesh {

        internal SubMesh() {
        }

        public uint FirstIndex { get; internal set; }
        public uint IndexCount { get; internal set; }
        public MeshTopology Topology { get; internal set; }
        public uint TriangleCount { get; internal set; }
        public uint FirstVertex { get; internal set; }
        public uint VertexCount { get; internal set; }
        public AABB BoundingBox { get; internal set; }
        internal uint FirstByte { get; set; }

    }
}
