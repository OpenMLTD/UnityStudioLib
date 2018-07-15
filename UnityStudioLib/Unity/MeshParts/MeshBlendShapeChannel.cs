using JetBrains.Annotations;

namespace UnityStudio.Unity.MeshParts {
    public sealed class MeshBlendShapeChannel {

        internal MeshBlendShapeChannel() {
        }

        [NotNull]
        public string Name { get; set; }
        public uint NameHash { get; set; }
        public int FrameIndex { get; set; }
        public int FrameCount { get; set; }

    }
}
