namespace UnityStudio.UnityEngine.MeshParts {
    public sealed class BlendShapeVertex {

        internal BlendShapeVertex() {
        }

        public Vector3 Vertex { get; internal set; }
        public Vector3 Normal { get; internal set; }
        public Vector3 Tangent { get; internal set; }
        public uint Index { get; internal set; }

    }
}
